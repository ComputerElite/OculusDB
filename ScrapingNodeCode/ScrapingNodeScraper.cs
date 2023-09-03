using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusDB.ScrapingMaster;
using OculusDB.Users;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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
    
    public ScrapingNodeScraperErrorTracker errorTracker { get; set; } = new ScrapingNodeScraperErrorTracker();


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
        try
        {
            GetEntitlements();
        }
        catch (Exception e)
        {
            Logger.Log("Failed to get entitlements for token " + currentToken + ". Error: " + e, LoggingType.Error);
        }
    }

    public void GetEntitlements()
    {
        GraphQLClient.log = false;
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
            if (!errorTracker.ContinueScraping())
            {
                scrapingNodeManager.status = ScrapingNodeStatus.RateLimited;
                int toSleep = (int)Math.Round((errorTracker.continueTime - DateTime.UtcNow).TotalMilliseconds);
                if (toSleep < 0)
                {
                    continue;
                }
                Logger.Log("Rate limited. Sleeping for " + toSleep + "ms. That's until " + errorTracker.continueTime.ToString("yyyy-MM-dd HH:mm:ss"));
                Thread.Sleep(toSleep);
                continue;
            }
            switch (scrapingTasks[0].scrapingTask)
            {
                case ScrapingTaskType.GetAllAppsToScrape:
                    TransmittingDone();
                    scrapingNodeManager.status = ScrapingNodeStatus.Scraping;
                    TransmitAndClearResultsIfPresent();
                    taskResult.altered = true;
                    taskResult.scrapingNodeTaskResultType = ScrapingNodeTaskResultType.FoundAppsToScrape;
                    try
                    {
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.MONTEREY));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.HOLLYWOOD));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.EUREKA));
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
                    TransmittingDone();
                    scrapingNodeManager.status = ScrapingNodeStatus.Scraping;
                    taskResult.scrapingNodeTaskResultType = ScrapingNodeTaskResultType.AppsScraped;
                    try
                    {
                        Scrape(scrapingTasks[0].appToScrape);
                    }
                    catch (Exception e)
                    {
                        ReportError(scrapingTasks[0].appToScrape, e);
                        Logger.Log("Failed to scrape " + scrapingTasks[0].appToScrape.appId + ": " + e, LoggingType.Error);
                    }
                    break;
                case ScrapingTaskType.WaitForResults:
                    scrapingNodeManager.status = ScrapingNodeStatus.WaitingForMasterServer;
                    Logger.Log("Waiting 20 seconds as results aren't processed yet");
                    Thread.Sleep(20000);
                    break;
            }
            // After task is done remove it from the scrapingTasks list
            scrapingTasks.RemoveAt(0);
            tasksDone++;
        }
        
        // After doing all tasks Transmit results if there are any
        TransmitAndClearResultsIfPresent();
    }

    private void ReportError(AppToScrape appToScrape, Exception exception)
    {
        // Ignore application is null errors
        if (exception.Message == "Application is null") return;
        ScrapingError error = new ScrapingError();
        error.appToScrape = appToScrape;
        error.errorMessage = exception.ToString();
        ScrapingErrorContainer c = new ScrapingErrorContainer();
        c.scrapingError = error;
        c.identification = scrapingNodeManager.GetIdentification();
        Logger.Log("Reporting error to MasterServer", LoggingType.Warning);
        try
        {
            string json = scrapingNodeManager.GetResponseOfPostRequest(scrapingNodeManager.config.masterAddress + "/api/v1/reportscrapingerror/", JsonSerializer.Serialize(c)).json;
        }
        catch (Exception e)
        {
            Logger.Log("Failed to report error to MasterServer: " + e, LoggingType.Error);
        }
    }

    private void TransmittingDone()
    {
        if (transmittingResults)
        {
            sw.Stop();
            Logger.Log("Server processed results in " + sw.ElapsedMilliseconds + "ms");
            SendHeartBeat();
        }
        transmittingResults = false;
    }

    public TimeSpan timeBetweenScrapes = new TimeSpan(2, 0, 0, 0);

    public string currentlyScraping = "";
    public void Scrape(AppToScrape app)
    {
        taskResult.altered = true;
        Application a = GraphQLClient.GetAppDetail(app.appId, app.headset).data.node;
        if (a == null)
        {
            errorTracker.AddError();
            throw new Exception("Application is null");
        }
        currentlyScraping = a.displayName + (app.priority ? " (Priority)" : "");
		if (!a.supported_hmd_platforms_enum.Contains(app.headset) && a.supported_hmd_platforms_enum.Count > 0) app.headset = a.supported_hmd_platforms_enum[0];
        long priceNumerical = -1;
        // Get price
        string currency = "";
        if (a.current_offer != null && a.current_offer.price != null)
        {
            priceNumerical = a.current_offer.price.offset_amount_numerical;
            currency = a.current_offer.price.currency;
        }
        else if(a.baseline_offer != null && a.baseline_offer.price != null)
        {
            priceNumerical = a.baseline_offer.price.offset_amount_numerical;
            currency = a.baseline_offer.price.currency;
        }
        
        
        Data<Application> d = GraphQLClient.GetDLCs(a.id);
        string packageName = "";
        List<DBVersion> connected = GetVersionsOfApp(a.id);
        bool addedApplication = false;
        Data<NodesPrimaryBinaryApplication> versions = GraphQLClient.AllVersionsOfApp(a.id);
        if (versions.data != null && versions.data.node != null && versions.data.node.primary_binaries != null &&
            versions.data.node.primary_binaries.nodes != null)
        {
            foreach (AndroidBinary b in versions.data.node.primary_binaries.nodes)
            {
                bool doPriorityForThisVersion = app.priority;
                DBVersion oldEntry = connected.FirstOrDefault(x => x.id == b.id);
                if (doPriorityForThisVersion)
                {
                    if (oldEntry != null)
                    {
                        // Only do priority scrape if last scrape is older than 2 days
                        if (DateTime.UtcNow - oldEntry.lastPriorityScrape < timeBetweenScrapes)
                        {
                            doPriorityForThisVersion = false;
                            Logger.Log(
                                "Skipping priority scrape of " + a.id + " v " + b.version +
                                " because last priority scrape was " +
                                (DateTime.UtcNow - oldEntry.lastPriorityScrape).TotalDays + " days ago",
                                LoggingType.Debug);
                        }
                    }
                }

                if (packageName == "")
                {
                    PlainData<AppBinaryInfoContainer> info = GraphQLClient.GetAssetFiles(a.id, b.versionCode);
                    if (info.data != null) packageName = info.data.app_binary_info.info[0].binary.package_name;
                }

                if (!addedApplication)
                {
                    AddApplication(a, app.headset, app.imageUrl, packageName, priceNumerical, currency);
                    addedApplication = true;
                }

                if (b != null && doPriorityForThisVersion)
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
                if ((!doPriorityForThisVersion || wasNull) && oldEntry != null)
                {
                    bin.changeLog = oldEntry.changeLog;
                }

                AddVersion(bin, a, app.headset, doPriorityForThisVersion ? null : oldEntry,
                    doPriorityForThisVersion);
            }
        }
        else
        {
            Logger.Log("No versions found for " + a.id + " " + a.displayName, LoggingType.Warning);
        }

        if (d.data != null && d.data.node != null && d.data.node.latest_supported_binary != null && d.data.node.latest_supported_binary.firstIapItems != null)
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
                
                // Skip dlc if it's free. Most likely indicates a bought DLC for which I do not figure out the correct price
                if (newDLC.priceOffsetNumerical <= 0) continue;
                if (a.current_offer == null || a.current_offer.price == null) continue; // Price not available

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
        Logger.Log("Scraped " + app.appId + (app.priority ? " (priority)" : ""));
    }

    public void AddDLCPack(AppItemBundle a, Headset h, Application app)
    {
        DBIAPItemPack dbdlcp = ObjectConverter.ConvertCopy<DBIAPItemPack, AppItemBundle, IAPItem>(a);
        dbdlcp.parentApplication.hmd = h;
        dbdlcp.parentApplication.displayName = app.displayName;
        if(dbdlcp.current_offer != null && dbdlcp.current_offer.price != null)
        {
            dbdlcp.current_offer.price.currency = GetOverrideCurrency(dbdlcp.current_offer.price.currency);
        }
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
        if (dbdlc.current_offer != null && dbdlc.current_offer.price != null)
        {
            dbdlc.current_offer.price.currency = GetOverrideCurrency(dbdlc.current_offer.price.currency);
        }
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
        dbv.__lastUpdated = DateTime.UtcNow;
            
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
        dbv.lastScrape = DateTime.UtcNow;
        if(isPriorityScrape) dbv.lastPriorityScrape = DateTime.UtcNow;
        
        taskResult.scraped.versions.Add(dbv);
    }

    public string GetOverrideCurrency(string currency)
    {
        if (scrapingNodeManager.config.overrideCurrency != "") return scrapingNodeManager.config.overrideCurrency;
        return currency;
    }

    public void AddApplication(Application a, Headset h, string image, string packageName, long correctPrice, string currency)
    {
        DBApplication dba = ObjectConverter.ConvertCopy<DBApplication, Application>(a);
        dba.hmd = h;
        dba.img = image;
        dba.priceOffsetNumerical = correctPrice;
        dba.priceFormatted = FormatPrice(correctPrice, currency);
        dba.packageName = packageName;
        dba.currency = GetOverrideCurrency(currency);
        if (dba.baseline_offer != null && dba.baseline_offer.price != null)
        {
            dba.baseline_offer.price.currency = GetOverrideCurrency(dba.baseline_offer.price.currency);
        }
        if (dba.current_offer != null && dba.current_offer.price != null)
        {
            dba.current_offer.price.currency = GetOverrideCurrency(dba.current_offer.price.currency);
        }
        if(dba.current_gift_offer != null && dba.current_gift_offer.price != null)
        {
            dba.current_gift_offer.price.currency = GetOverrideCurrency(dba.current_gift_offer.price.currency);
        }
        taskResult.scraped.applications.Add(dba);
        DBAppImage dbi = DownloadImage(dba);
        if (dbi != null)
        {
            taskResult.scraped.imgs.Add(dbi);
        }
    }

    public DBAppImage DownloadImage(DBApplication a)
    {
        //Logger.Log("Downloading image of " + a.id + " from " + a.img);
        try
        {
            string ext = Path.GetExtension(a.img.Split('?')[0]);
            if (a.img == "") return null;
            WebClient c = new WebClient();
            c.Headers.Add("user-agent", OculusDBEnvironment.userAgent);
            byte[] data = c.DownloadData(a.img);
            // Try converting image to webp format
            try
            {
                using (var img = Image.Load(data))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int width = img.Width;
                        int height = img.Height;
                        if (width > 1024 || height > 1024)
                        {
                            if (width > height)
                            {
                                height = (int) (height * (1024f / width));
                                width = 1024;
                            }
                            else
                            {
                                width = (int) (width * (1024f / height));
                                height = 1024;
                            }
                        }
                        img.Mutate(x => x.Resize(width, height));
                        img.Save(ms, new WebpEncoder());
                        data = ms.ToArray();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Couldn't convert image to webp or scale it to max 1024x1024:\n" + e, LoggingType.Warning);
                return null;
            }
            DBAppImage dbi = new DBAppImage();
            dbi.data = data;
            dbi.mimeType = HttpServer.GetContentTpe("image" + ext);
            dbi.appId = a.id;
            return dbi;
        } catch(Exception e)
        {
            Logger.Log("Couldn't download image of " + a.id + ":\n" + e.ToString, LoggingType.Warning);
        }

        return null;
    }

    public List<DBVersion> GetVersionsOfApp(string appId)
    {
        string json = scrapingNodeManager.GetResponseOfPostRequest(scrapingNodeManager.config.masterAddress + "/api/v1/versions/" + appId,
            JsonSerializer.Serialize(scrapingNodeManager.GetIdentification())).json;
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
        if (currency == "AUD") symbol = "$";
        if (currency == "EUR") symbol = "€";
        string price = symbol + String.Format("{0:0.00}", offsetAmount / 100.0);
        
        return price;
    }

    Stopwatch sw = Stopwatch.StartNew();
    public void TransmitAndClearResultsIfPresent()
    {
        if (!taskResult.altered) return;
        scrapingNodeManager.status = ScrapingNodeStatus.TransmittingResults;
        SendHeartBeat();
        transmittingResults = true;
        Logger.Log("Transmitting results");
        taskResult.identification = scrapingNodeManager.GetIdentification();
        ScrapingProcessedResult r;
        sw = Stopwatch.StartNew();
        try
        {
            string json = scrapingNodeManager.GetResponseOfPostRequest(scrapingNodeManager.config.masterAddress + "/api/v1/taskresults", JsonSerializer.Serialize(taskResult)).json;
            r = JsonSerializer.Deserialize<ScrapingProcessedResult>(json);
        }
        catch (Exception e)
        {
            Logger.Log("Error while transmitting results: " + e, LoggingType.Error);
        }
        taskResult = new ScrapingNodeTaskResult();
        // Sleep 500 ms so Server can defo mark the node as processing
        Thread.Sleep(500);
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
                appsToScrape.Add(new AppToScrape { currency = GetCurrency(), headset = h, appId = a.id, priority = false, imageUrl = a.cover_square_image.uri });
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
                appsToScrape.Add(new AppToScrape { currency = GetCurrency(), appId = id, imageUrl = a.image_url, priority = false, headset = Headset.HOLLYWOOD });
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

    public void SendHeartBeat()
    {
        ScrapingNodeHeartBeat beat = new ScrapingNodeHeartBeat();
        beat.identification = scrapingNodeManager.GetIdentification();
        beat.snapshot.scrapingStatus = scrapingNodeManager.status;
        beat.snapshot.totalTasks = totalTasks;
        beat.snapshot.scrapingContinueTime = errorTracker.continueTime;
        beat.snapshot.doneTasks = tasksDone;
        beat.snapshot.currentlyScraping = currentlyScraping;
        beat.SetQueuedDocuments(taskResult);
        Logger.Log("Sending heartbeat. Status: " + Enum.GetName(typeof(ScrapingNodeStatus), scrapingNodeManager.status));
        try
        {
            ScrapingNodePostResponse r = scrapingNodeManager.GetResponseOfPostRequest(
                scrapingNodeManager.config.masterAddress + "/api/v1/heartbeat", JsonSerializer.Serialize(beat));
        }
        catch (Exception e)
        {
            Logger.Log("Error while sending heartbeat: " + e, LoggingType.Error);
        }
    }

    private Dictionary<int, string> currencyTokenDict = new();
    public string GetCurrency()
    {
        // To get the currency of the node just request beat saber from oculus and check the price
        if(scrapingNodeManager.config.overrideCurrency != "") return scrapingNodeManager.config.overrideCurrency;
        if(currencyTokenDict.ContainsKey(currentToken)) return currencyTokenDict[currentToken];
        try
        {
            GraphQLClient.log = false;
            Application a = GraphQLClient.GetAppDetail("2448060205267927", Headset.MONTEREY).data.node;
            string currency = a.current_offer.price.currency;
            currencyTokenDict.Add(currentToken, currency);
            return currency;
        }
        catch (Exception e)
        {
            Logger.Log("Error while getting currency: " + e, LoggingType.Error);
            return "";
        }
    }
}