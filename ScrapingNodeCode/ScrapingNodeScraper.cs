using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusDB.ScrapingMaster;
using OculusDB.Users;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeScraper
{
    public List<ScrapingTask> scrapingTasks { get; set; } = new List<ScrapingTask>();
    public ScrapingNodeTaskResult taskResult { get; set; } = new ScrapingNodeTaskResult();
    public ScrapingNodeManager scrapingNodeManager { get; set; } = new ScrapingNodeManager();
    public List<Entitlement> userEntitlements { get; set; } = new List<Entitlement>();
    public bool transmittingResults { get; set; } = false;
    public int currentToken = 0;
    public int totalTasks = 0;
    public int tasksDone = 0;


    public ScrapingNodeScraper(ScrapingNodeManager manager)
    {
        scrapingNodeManager = manager;
    }

    public void ChangeToken()
    {
        // This will set the token globally, all Scraping Nodes running via this process will use the same token. Might lead to problems down the line.
        currentToken++;
        currentToken %= scrapingNodeManager.config.oculusTokens.Count;
        GraphQLClient.oculusStoreToken = scrapingNodeManager.config.oculusTokens[currentToken];
        GetEntitlements();
    }

    public void GetEntitlements()
    {
        Logger.Log("Getting entitlements of token at " + currentToken);
        ViewerData<OculusUserWrapper> user = GraphQLClient.GetActiveEntitelments();
        if(user == null || user.data == null || user.data.viewer == null || user.data.viewer.user == null || user.data.viewer.user.active_entitlements == null ||user.data.viewer.user.active_entitlements.nodes == null)
        {
            throw new Exception("Fetching of active entitlements failed");
        }
        userEntitlements = user.data.viewer.user.active_entitlements.nodes;
        Logger.Log("Got " + userEntitlements.Count + " entitlements for " + user.data.viewer.user.alias);
    }

    public void DoTasks()
    {
        totalTasks = scrapingTasks.Count;
        tasksDone = 0;
        taskResult = new ScrapingNodeTaskResult();
        while (scrapingTasks.Count > 0)
        {
            scrapingNodeManager.status = ScrapingNodeStatus.Scraping;
            switch (scrapingTasks[0].scrapingTask)
            {
                case ScrapingTaskType.GetAllAppsToScrape:
                    TransmitAndClearResultsIfPresent();
                    taskResult.altered = true;
                    taskResult.scrapingNodeTaskResultType = ScrapingNodeTaskResultType.FoundAppsToScrape;
                    try
                    {
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.HOLLYWOOD));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.RIFT));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.GEARVR));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.PACIFIC));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.SEACLIFF));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeFromApplab());
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Couldn't collect apps to scrape: " + e, LoggingType.Error);
                        Logger.Log("Informing server of error");
                        taskResult.scrapingNodeTaskResultType =
                            ScrapingNodeTaskResultType.ErrorWhileRequestingAppsToScrape;
                    }
                    
                    break;
                case ScrapingTaskType.ScrapeApp:
                    taskResult.scrapingNodeTaskResultType = ScrapingNodeTaskResultType.AppsScraped;
                    try
                    {
                        Scrape(scrapingTasks[0].appToScrape);
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Failed to scrape " + scrapingTasks[0].appToScrape.appId + ": " + e, LoggingType.Error);
                    }
                    break;
            }
            // After task is done remove it from the scrapingTasks list
            scrapingTasks.RemoveAt(0);
            tasksDone++;
        }
        
        // After doing all tasks Transmit results if there are any
        TransmitAndClearResultsIfPresent();
    }
    public TimeSpan timeBetweenScrapes = new TimeSpan(2, 0, 0, 0);

    public void Scrape(AppToScrape app)
    {
        taskResult.altered = true;
        Application a = GraphQLClient.GetAppDetail(app.appId, app.headset).data.node;
        if (a == null) throw new Exception("Application is null");
		if (!a.supported_hmd_platforms_enum.Contains(app.headset)) app.headset = a.supported_hmd_platforms_enum[0];
        long priceNumerical = 0;
        // Get price
        if (a.current_offer != null) priceNumerical = Convert.ToInt64(a.current_offer.price.offset_amount);

        UserEntitlement ownsApp = GetEntitlementStatusOfAppOrDLC(a.id);
        if (ownsApp == UserEntitlement.FAILED)
        {
            if (a.current_offer != null && a.baseline_offer != null)
            {
                // If price of baseline and current is not the same and there is no discount then the user probably owns the app.
                // Owning an app sets it current_offer to 0 currency but baseline_offer still contains the price
                // So if the user owns the app use the baseline price. If not use the current_price
                // That way discounts for the apps the user owns can't be tracked. I love oculus
                if (a.current_offer.price.offset_amount != a.baseline_offer.price.offset_amount && a.current_offer.promo_benefit == null)
                {
                    priceNumerical = Convert.ToInt64(a.baseline_offer.price.offset_amount);
                }
            }
        }
        else if (ownsApp == UserEntitlement.OWNED)
        {
            if (a.baseline_offer != null) priceNumerical = Convert.ToInt64(a.baseline_offer.price.offset_amount);
        }
        
        
        Data<Application> d = GraphQLClient.GetDLCs(a.id);
        string packageName = "";
        List<DBVersion> connected = GetVersionsOfApp(a.id);
        bool addedApplication = false;
        foreach (AndroidBinary b in GraphQLClient.AllVersionsOfApp(a.id).data.node.primary_binaries.nodes)
        {
            bool doPriorityForThisVersion = app.priority;
            DBVersion oldEntry = connected.FirstOrDefault(x => x.id == b.id);
            if (doPriorityForThisVersion)
            {
                if (oldEntry != null)
                {
                    // Only do priority scrape if last scrape is older than 2 days
                    if (DateTime.Now - oldEntry.lastPriorityScrape < timeBetweenScrapes)
                    {
                        doPriorityForThisVersion = false;
                        Logger.Log("Skipping priority scrape of " + a.id + " v " + b.version + " because last priority scrape was " + (DateTime.Now - oldEntry.lastPriorityScrape).TotalDays + " days ago", LoggingType.Debug);
                    }
                }
            }
            if(packageName == "")
            {
                PlainData<AppBinaryInfoContainer> info = GraphQLClient.GetAssetFiles(a.id, b.versionCode);
                if(info.data != null) packageName = info.data.app_binary_info.info[0].binary.package_name;
			}
            if(!addedApplication)
            {
				AddApplication(a, app.headset, app.imageUrl, packageName, priceNumerical, a.current_offer.price.currency);
                addedApplication = true;
			}
            if(b != null && doPriorityForThisVersion)
            {
                Logger.Log("Scraping v " + b.version, LoggingType.Important);
			}
			AndroidBinary bin = doPriorityForThisVersion ? GraphQLClient.GetBinaryDetails(b.id).data.node : b;
            bool wasNull = false;
            if (bin == null)
            {
                if (!doPriorityForThisVersion || b == null) continue; // skip if version was unable to be fetched
                wasNull = true;
                bin = b;
			}
            // Preserve changelogs and obbs across scrapes by:
            // - Don't delete versions after scrape
            // - If not priority scrape enter changelog and obb of most recent versions
            if((!doPriorityForThisVersion || wasNull) && oldEntry != null)
            {
                bin.changeLog = oldEntry.changeLog;
            }

            AddVersion(bin, a, app.headset, doPriorityForThisVersion ? null : oldEntry,
                doPriorityForThisVersion);
        }
        if (d.data.node.latest_supported_binary != null && d.data.node.latest_supported_binary.firstIapItems != null)
        {
            foreach (Node<AppItemBundle> dlc in d.data.node.latest_supported_binary.firstIapItems.edges)
            {
                // For whatever reason Oculus sets parentApplication wrong. e. g. Beat Saber for Rift: it sets Beat Saber for quest
                dlc.node.parentApplication.canonicalName = a.canonicalName;
                dlc.node.parentApplication.id = a.id;
                
                // DBActivityNewDLC is needed as I use it for the price offset
                DBActivityNewDLC newDLC = new DBActivityNewDLC();
                newDLC.id = dlc.node.id;
                newDLC.parentApplication.id = a.id;
                newDLC.parentApplication.hmd = app.headset;
                newDLC.parentApplication.canonicalName = a.canonicalName;
                newDLC.parentApplication.displayName = a.displayName;
                newDLC.displayName = dlc.node.display_name;
                newDLC.displayShortDescription = dlc.node.display_short_description;
                newDLC.latestAssetFileId = dlc.node.latest_supported_asset_file != null ? dlc.node.latest_supported_asset_file.id : "";
                newDLC.priceOffset = dlc.node.current_offer.price.offset_amount;
                
                if (dlc.node.IsIAPItem())
                {
                    // Handle DLC entitlement check 
                    UserEntitlement ownsDlc = GetEntitlementStatusOfAppOrDLC(a.id, dlc.node.id, dlc.node.display_name);

                    if (ownsDlc == UserEntitlement.FAILED && newDLC.priceOffsetNumerical <= 0 || ownsDlc == UserEntitlement.OWNED && newDLC.priceOffsetNumerical <= 0) continue;
                }
                else
                {
                    // Handle DLC Pack entitlement check
                    bool ownsAll = true;
                    foreach (Node<IAPItem> includedDlc in dlc.node.bundle_items.edges)
                    {
                        UserEntitlement ownsDlc = GetEntitlementStatusOfAppOrDLC(a.id, includedDlc.node.id, includedDlc.node.display_name);
                        if (ownsDlc == UserEntitlement.FAILED && newDLC.priceOffsetNumerical <= 0 ||
                            ownsDlc == UserEntitlement.OWNED && newDLC.priceOffsetNumerical <= 0)
                        {
                            ownsAll = false;
                            break;
                        }
                    }

                    // If not all DLCs in the pack are owned, skip the dlc pack to avoid price changing
                    if (!ownsAll) continue;
                }
                

                newDLC.priceFormatted = FormatPrice(newDLC.priceOffsetNumerical, a.current_offer.price.currency);
                if (dlc.node.IsIAPItem())
                {
                    AddDLC(dlc.node, app.headset);
                }
                else
                {
                    AddDLCPack(dlc.node, app.headset, a);
                }
            }
        }
        Logger.Log("Scraped " + app.appId);
    }

    public void AddDLCPack(AppItemBundle a, Headset h, Application app)
    {
        DBIAPItemPack dbdlcp = ObjectConverter.ConvertCopy<DBIAPItemPack, AppItemBundle, IAPItem>(a);
        dbdlcp.parentApplication.hmd = h;
        dbdlcp.parentApplication.displayName = app.displayName;
        foreach(Node<IAPItem> i in a.bundle_items.edges)
        {
            DBItemId id = new DBItemId();
            id.id = i.node.id;
            dbdlcp.bundle_items.Add(id);
        }
        
        taskResult.scraped.dlcPacks.Add(dbdlcp);
    }

    public void AddDLC(AppItemBundle a, Headset h)
    {
        DBIAPItem dbdlc = ObjectConverter.ConvertCopy<DBIAPItem, IAPItem>(a);
        dbdlc.parentApplication.hmd = h;
        dbdlc.latestAssetFileId = a.latest_supported_asset_file != null ? a.latest_supported_asset_file.id : "";
        taskResult.scraped.dlcs.Add(dbdlc);
    }

    public void AddVersion(AndroidBinary a, Application app, Headset h, DBVersion oldEntry, bool isPriorityScrape)
    {
        DBVersion dbv = ObjectConverter.ConvertCopy<DBVersion, AndroidBinary>(a);
        dbv.parentApplication.id = app.id;
        dbv.binary_release_channels = new Nodes<ReleaseChannelWithoutLatestSupportedBinary>();
        dbv.binary_release_channels.nodes =
            a.binary_release_channels.nodes.ConvertAll(x => (ReleaseChannelWithoutLatestSupportedBinary)x);
        dbv.parentApplication.hmd = h;
        dbv.parentApplication.displayName = app.displayName;
        dbv.parentApplication.canonicalName = app.canonicalName;
        dbv.__lastUpdated = DateTime.Now;
            
        if(oldEntry == null)
        {
            if (a.obb_binary != null)
            {
                if (dbv.obbList == null) dbv.obbList = new List<OBBBinary>();
                dbv.obbList.Add(ObjectConverter.ConvertCopy<OBBBinary, AssetFile>(a.obb_binary));
            }
            foreach (AssetFile f in a.asset_files.nodes)
            {
                if (dbv.obbList == null) dbv.obbList = new List<OBBBinary>();
                if (f.is_required) dbv.obbList.Add(ObjectConverter.ConvertCopy<OBBBinary, AssetFile>(f));
            }
        } else
        {
            dbv.obbList = oldEntry.obbList;
            dbv.lastPriorityScrape = oldEntry.lastPriorityScrape;
        }
        dbv.lastScrape = DateTime.Now;
        if(isPriorityScrape) dbv.lastPriorityScrape = DateTime.Now;
        
        taskResult.scraped.versions.Add(dbv);
    }

    public void AddApplication(Application a, Headset h, string image, string packageName, long correctPrice, string currency)
    {
        DBApplication dba = ObjectConverter.ConvertCopy<DBApplication, Application>(a);
        dba.hmd = h;
        dba.img = image;
        dba.priceOffsetNumerical = correctPrice;
        dba.priceFormatted = FormatPrice(correctPrice, currency);
        dba.packageName = packageName;
        taskResult.scraped.applications.Add(dba);
        DBAppImage dbi = DownloadImage(dba);
        if (dbi != null)
        {
            taskResult.scraped.imgs.Add(dbi);
        }
    }

    private DBAppImage DownloadImage(DBApplication dba)
    {
        throw new NotImplementedException();
    }

    public List<DBVersion> GetVersionsOfApp(string appId)
    {
        string json = scrapingNodeManager.GetResponseOfPostRequest(scrapingNodeManager.config.masterAddress + "/api/v1/versions/" + appId,
            JsonSerializer.Serialize(scrapingNodeManager.GetIdentification()));
        return JsonSerializer.Deserialize<List<DBVersion>>(json);
    }

    public UserEntitlement GetEntitlementStatusOfAppOrDLC(string appId, string dlcId = null, string dlcName = "")
    {
        if (userEntitlements.Count <= 0) return UserEntitlement.FAILED;
        foreach(Entitlement entitlement in userEntitlements)
        {
            if(entitlement.item.id == appId)
            {
                if(dlcId == null) return UserEntitlement.OWNED;
                foreach(IAPEntitlement dlc in entitlement.item.active_dlc_entitlements)
                {
                    if(dlc.item.id == dlcId ||dlc.item.display_name == dlcName)
                    {
                        return UserEntitlement.OWNED;
                    }
                }
                return UserEntitlement.NOTOWNED;
            }
        }
        return UserEntitlement.NOTOWNED;
    }

    public string FormatPrice(long offsetAmount, string currency)
        {
            string symbol = "";
            if (currency == "USD") symbol = "$";
            if (currency == "EUR") symbol = "€";
            string price = symbol + String.Format("{0:0.00}", offsetAmount / 100.0);
            
            return price;
        }

    public void TransmitAndClearResultsIfPresent()
    {
        if (!taskResult.altered) return;
        scrapingNodeManager.status = ScrapingNodeStatus.TransmittingResults;
        SendHeartBeat();
        transmittingResults = true;
        Logger.Log("Transmitting results");
        taskResult.identification = scrapingNodeManager.GetIdentification();
        ScrapingProcessedResult r;
        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            string json = scrapingNodeManager.GetResponseOfPostRequest(scrapingNodeManager.config.masterAddress + "/api/v1/taskresults", JsonSerializer.Serialize(taskResult));
            r = JsonSerializer.Deserialize<ScrapingProcessedResult>(json);
            Logger.Log("Got response from scraping master after " + sw.ElapsedMilliseconds + "ms: " + r.msg);
        }
        catch (Exception e)
        {
            Logger.Log("Error while transmitting results: " + e, LoggingType.Error);
        }
        taskResult = new ScrapingNodeTaskResult();
        transmittingResults = false;
    }
    
    public List<AppToScrape> CollectAppsToScrapeForHeadset(Headset h)
    {
        List<AppToScrape> appsToScrape = new List<AppToScrape>();
        int apps = 0;
        Logger.Log("Adding apps to scrape for " + HeadsetTools.GetHeadsetCodeName(h));
        try
        {
            foreach (Application a in OculusInteractor.EnumerateAllApplications(h))
            {
                apps++;
                appsToScrape.Add(new AppToScrape { headset = h, appId = a.id, priority = false, imageUrl = a.cover_square_image.uri });
            }
        } catch(Exception e)
        {
            Logger.Log(e.ToString(), LoggingType.Warning);
        }
        Logger.Log("Found " + apps + " apps to scrape for " + HeadsetTools.GetHeadsetCodeName(h));
        return appsToScrape;
    }
    
    public List<AppToScrape> CollectAppsToScrapeFromApplab()
    {
        List<AppToScrape> appsToScrape = new List<AppToScrape>();
        WebClient c = new WebClient();
        int lastCount = -1;
        bool didIncrease = true;
        List<SidequestApplabGame> s = new List<SidequestApplabGame>();
        while(didIncrease)
        {
            s.AddRange(JsonSerializer.Deserialize<List<SidequestApplabGame>>(c.DownloadString("https://api.sidequestvr.com/v2/apps?limit=1000&skip=" + s.Count + "&is_app_lab=true&has_oculus_url=true&sortOn=downloads&descending=true")));
            didIncrease = lastCount != s.Count;
            lastCount = s.Count;
        }
        Logger.Log("queued " + lastCount + " applab apps");
        foreach (SidequestApplabGame a in s)
        {
            string id = a.oculus_url.Replace("/?utm_source=sidequest", "").Replace("?utm_source=sq_pdp&utm_medium=sq_pdp&utm_campaign=sq_pdp&channel=sq_pdp", "").Replace("https://www.oculus.com/experiences/quest/", "").Replace("/", "");
            if (id.Length <= 16)
            {
                appsToScrape.Add(new AppToScrape { appId = id, imageUrl = a.image_url, priority = false, headset = Headset.HOLLYWOOD });
            }
        }

        return appsToScrape;
    }

    public void HeartBeatLoop()
    {
        while (true)
        {
            SendHeartBeat();
            Task.Delay(30 * 1000).Wait();
        }
    }

    private void SendHeartBeat()
    {
        // Do not send heartbeats while transmitting results
        if(transmittingResults) return;
        Logger.Log("Sending heartbeat");
        ScrapingNodeHeartBeat beat = new ScrapingNodeHeartBeat();
        beat.identification = scrapingNodeManager.GetIdentification();
        beat.snapshot.scrapingStatus = scrapingNodeManager.status;
        beat.snapshot.totalTasks = totalTasks;
        beat.snapshot.doneTasks = tasksDone;
        beat.SetQueuedDocuments(taskResult);
        string json = scrapingNodeManager.GetResponseOfPostRequest(scrapingNodeManager.config.masterAddress + "/api/v1/heartbeat", JsonSerializer.Serialize(beat));
    }
}