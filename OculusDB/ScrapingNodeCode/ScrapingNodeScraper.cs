using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusDB.ObjectConverters;
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
    public bool oAuthException = false;

    public string scrapingNodeId
    {
        get
        {
            return scrapingNodeManager.config.scrapingNode.scrapingNodeId;
        }
    }

    public ScrapingNodeScraperErrorTracker errorTracker { get; set; } = new ScrapingNodeScraperErrorTracker();


    public ScrapingNodeScraper(ScrapingNodeManager manager)
    {
        scrapingNodeManager = manager; 
    }

    public void Init()
    {
        // Register OAuthError
        GraphQLClient.OnOAuthException += message =>
        {
            // Token is invalid. Report to MasterServer
            Logger.Log("OAuthException: " + message, LoggingType.Error);
            Logger.Log("Node will now stop scraping. Please update your token via 'dotnet OculusDB.dll --so <Oculus Token>'", LoggingType.Error);
            scrapingNodeManager.status = ScrapingNodeStatus.OAuthException;
            SendHeartBeat();
            oAuthException = true;
            Environment.Exit(1);
        };
    }

    public void ChangeToken()
    {
        // This will set the token globally, all Scraping Nodes running via this process will use the same token. Might lead to problems down the line.
        currentToken++;
        currentToken %= scrapingNodeManager.config.oculusTokens.Count;
        GraphQLClient.userToken = scrapingNodeManager.config.oculusTokens[currentToken];
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
            if (oAuthException)
            {
                Logger.Log("Terminating scraping. OAuthException occurred");
                return;
            }
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
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.PANTHER));
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
        // Add application
        Application? applicationFromDeveloper = GraphQLClient.AppDetailsDeveloperAll(app.appId).data.node;
        Application? applicationFromStore = GraphQLClient.AppDetailsMetaStore(app.appId).data.item;


        if (applicationFromStore == null && applicationFromDeveloper == null)
        {
            errorTracker.AddError();
            ReportApplicationNull(app);
            return;
        }
        currentlyScraping = applicationFromStore.displayName + (app.priority ? " (Priority)" : ""); // throw an error here on purpose

        
        DBApplication dbApp = OculusConverter.AddScrapingNodeName(OculusConverter.Application(applicationFromDeveloper, applicationFromStore), scrapingNodeId);
        
        List<DBOffer?> offers = new List<DBOffer?>();
        offers.Add(OculusConverter.AddScrapingNodeName(OculusConverter.Price(applicationFromStore.current_offer, dbApp), scrapingNodeId));
        List<DBIapItemPack?> dlcPacks = new List<DBIapItemPack?>();
        // Get DLC Packs and prices
        // Add DLCs
        List<DBIapItem?> iapItems = new List<DBIapItem?>();
        int i = 0;
        bool dlcsAdded = false;
        if (dbApp.grouping != null)
        {
            try
            {
                foreach (IAPItem iapItem in OculusInteractor.EnumerateAllDLCs(dbApp.grouping.id))
                {
                    i++;
                    iapItems.Add(OculusConverter.AddScrapingNodeName(OculusConverter.IAPItem(iapItem, dbApp), scrapingNodeId));
                }

                dlcsAdded = true;
            }
            catch (Exception e)
            {
                dbApp.errors.Add(new DBError
                {
                    type = DBErrorType.CouldNotScrapeIapsFully,
                    reason = DBErrorReason.DeveloperApplicationNull,
                    message = "Couldn't scrape IAPs because developer api returned nothing. Some info in IAPs may be missing and will be marked with null or -1"
                });
            }
        }
        else
        {
            dbApp.errors.Add(new DBError
            {
                type = DBErrorType.CouldNotScrapeIaps,
                reason = dbApp.grouping == null ? DBErrorReason.GroupingNull : DBErrorReason.Unknown,
                message = "Couldn't scrape IAPs because grouping is null"
            });
        }
        Data<Application> dlcApplication = GraphQLClient.GetDLCs(dbApp.id);
        if (dlcApplication.data.node != null && dlcApplication.data.node.latest_supported_binary != null && dlcApplication.data.node.latest_supported_binary.firstIapItems != null)
        {
            foreach (Node<AppItemBundle> dlc in dlcApplication.data.node.latest_supported_binary.firstIapItems.edges)
            {
                if (dlc.node.typename_enum == OculusTypeName.IAPItem)
                {
                    // dlc, extract price and add short description
                    if (iapItems.Any(x => x.id == dlc.node.id))
                    {
                        iapItems.FirstOrDefault(x => x.id == dlc.node.id).displayShortDescription = dlc.node.display_short_description;
                    }
                    else
                    {
                        if (dlcsAdded)
                        {
                            dbApp.errors.Add(new DBError
                            {
                                type = DBErrorType.StoreDlcsNotFoundInExistingDlcs,
                                reason = DBErrorReason.DlcNotInDlcList,
                                message = "DLC " + dlc.node.display_name + " (" + dlc.node.id + ") not found in store existing DLCs"
                            });
                        }
                        else
                        {
                            // If dlcs weren't added by developer api just add them from store
                            Logger.Log(JsonSerializer.Serialize(dlc));
                            IAPItem iap = new IAPItem();
                            // Turns out I just wasted 2 hours of my life cause I was trying to scrape an version instead of an app
                            // However I'll leave the code in there cause after all it was a lot of work
                            // FUCK YOU PAST ME FOR NOT CHECKING THE LOG SOMEONE SENT THOUROUGHLY
                            // AND FOR MAKING THE LOGS CONFUSING
                            // HUAIGEMPGHEPAHGAOUEMPAHAEMPGHUPEAHGAEUMPIGHIAHGuidsghspihguspiehapgheasu
                        }
                    }
                    offers.Add(
                        OculusConverter.AddScrapingNodeName(OculusConverter.Price(dlc.node.current_offer, dbApp),
                            scrapingNodeId));
                }
                else
                {
                    offers.Add(
                        OculusConverter.AddScrapingNodeName(OculusConverter.Price(dlc.node.current_offer, dbApp),
                            scrapingNodeId));
                    dlcPacks.Add(OculusConverter.AddScrapingNodeName(OculusConverter.IAPItemPack(dlc.node, dbApp.grouping), scrapingNodeId));
                    // dlc pack, extract dlc pack and price
                }
            }
        }
        
        
        // Get Versions
        List<DBVersion?> versions = new List<DBVersion?>();
        List<DBVersion> existingVersions = GetVersionsOfApp(dbApp.id);
        string? packageName = null;
        bool triedToGetPackageName = false;
        foreach (OculusBinary binary in OculusInteractor.EnumerateAllVersions(dbApp.id))
        {
            bool doPriority = app.priority;
            DBVersion? existing = existingVersions.FirstOrDefault(x => x.id == binary.id);
            Logger.Log("existing version found: " + (existing != null));
            if (existing != null)
            {
                if (existing.__lastPriorityScrape != null && (DateTime.UtcNow - existing.__lastPriorityScrape).Value.TotalDays < 1)
                {
                    Logger.Log("Not scraping " + binary.id + " as it was scraped in the last 24 hours");
                    doPriority = false;
                }
            }
            else
            {
                // first scrape of an version should be priority
                doPriority = true;
            }

            if (!triedToGetPackageName)
            {
                doPriority = true;
                Logger.Log("Doing priority to get package name");
            }
            if(doPriority) Logger.Log(binary.version + " - " + binary.versionCode + " (" + binary.id + ")");
            
            DBVersion v = OculusConverter.AddScrapingNodeName(OculusConverter.Version(doPriority ? GraphQLClient.GetMoreBinaryDetails(binary.id).data.node : null, binary, applicationFromDeveloper, dbApp, existing), scrapingNodeId);
            if (doPriority && !triedToGetPackageName)
            {
                triedToGetPackageName = true;
                packageName = v.packageName;
            }

            v.packageName = packageName;
            versions.Add(v);
        }
        
        

        // Add application packageName
        
        dbApp.packageName = packageName;
        
        
        // Add Achievements
        List<DBAchievement?> achievements = new List<DBAchievement?>();
        try
        {
            foreach (AchievementDefinition achievement in OculusInteractor.EnumerateAllAchievements(dbApp.id))
            {
                achievements.Add(OculusConverter.AddScrapingNodeName(OculusConverter.Achievement(achievement, dbApp), scrapingNodeId));
            }
        } catch (Exception e)
        {
            dbApp.errors.Add(new DBError
            {
                type = DBErrorType.CouldNotScrapeAchievements,
                reason = dbApp.grouping == null ? DBErrorReason.GroupingNull : DBErrorReason.Unknown,
                message =e.ToString()
            });
        }
        
        
        DBAppImage? dbi = DownloadImage(dbApp);
        if (dbi != null)
        {
            taskResult.scraped.imgs.Add(dbi);
        }
        
        taskResult.scraped.offers.AddRange(offers.Where(x => x != null).ToList().ConvertAll(x => (DBOffer)x));
        taskResult.scraped.iapItemPacks.AddRange(dlcPacks.Where(x => x != null).ToList().ConvertAll(x => (DBIapItemPack)x));
        taskResult.scraped.iapItems.AddRange(iapItems.Where(x => x != null).ToList().ConvertAll(x => (DBIapItem)x));
        taskResult.scraped.versions.AddRange(versions.Where(x => x != null).ToList().ConvertAll(x => (DBVersion)x));
        taskResult.scraped.achievements.AddRange(achievements.Where(x => x != null).ToList().ConvertAll(x => (DBAchievement)x));
        taskResult.scraped.applications.Add(dbApp);
    }

    public void ReportApplicationNull(AppToScrape app)
    {
        try
        {
            ScrapingNodePostResponse r = scrapingNodeManager.GetResponseOfPostRequest(scrapingNodeManager.config.masterAddress + "/api/v1/applicationnull",
                JsonSerializer.Serialize(new ScrapingNodeApplicationNull
                {
                    identification = scrapingNodeManager.GetIdentification(),
                    applicationId = app.appId
                }));
            if (r.status == 200)
            {
                Logger.Log("Reported application null for " + app.appId);
            }
            else
            {
                Logger.Log("Failed to report application null for " + app.appId + ". Server responded with status code " + r.status, LoggingType.Error);
            }
        }
        catch (Exception e)
        {
            Logger.Log("Erro sending request to report application null for " + app.appId + ": " + e, LoggingType.Error);
        }
    }

    public string GetOverrideCurrency(string currency)
    {
        if (scrapingNodeManager.config.overrideCurrency != "") return scrapingNodeManager.config.overrideCurrency;
        return currency;
    }

    public DBAppImage? DownloadImage(DBApplication a)
    {
        //Logger.Log("Downloading image of " + a.id + " from " + a.img);
        try
        {
            string ext = Path.GetExtension(a.oculusImageUrl.Split('?')[0]);
            if (a.oculusImageUrl == "") return null;
            WebClient c = new WebClient();
            c.Headers.Add("user-agent", OculusDBEnvironment.userAgent);
            byte[] data = c.DownloadData(a.oculusImageUrl);
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

            if (data.Length > 500 * 1024)
            {
                Logger.Log("Converted image larger than 500 KB. Skipping", LoggingType.Warning);
                return null;
            }
            DBAppImage dbi = new DBAppImage();
            dbi.data = data;
            dbi.mimeType = HttpServer.GetContentTpe("image" + ext);
            dbi.parentApplication = OculusConverter.ParentApplication(a);
            return OculusConverter.AddScrapingNodeName(dbi, scrapingNodeId);
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
                appsToScrape.Add(new AppToScrape { currency = GetCurrency(), appId = a.id, priority = false, canonicalName = a.canonicalName});
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
                appsToScrape.Add(new AppToScrape { currency = GetCurrency(), appId = id, priority = false });
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
        beat.snapshot.isPriorityScrape = scrapingNodeManager.config.isPriorityScrape;
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
            Application a = GraphQLClient.GetAppDetail("2448060205267927", Headset.MONTEREY).data.item;
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