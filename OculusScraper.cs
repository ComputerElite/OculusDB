using ComputerUtils.Logging;
using ComputerUtils.Updating;
using ComputerUtils.VarUtils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusDB.Users;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace OculusDB
{
    public class OculusScraper
    {
        public static Config config { get
            {
                return OculusDBEnvironment.config;
            } }
        public static int totalScrapeThreads = 0;
        public static int failedApps = 0;
        public static int doneScrapeThreads = 0;
        public static DateTime lastUpdate = DateTime.Now;
        public static List<PriorityScrape> priorityScrapeApps = new List<PriorityScrape>();
        public static bool priorityScrapeRunning = false;
        public static DateTime scrapeResumeTime = DateTime.MinValue;

        public const int maxAppsToDo = 2000;
        public const int maxAppsToFail = 25;
        public const int minutesPause = 120;

        public static List<Entitlement> userEntitlements { get; set; } = new List<Entitlement>();

        public static void AddApp(string id, Headset headset)
        {
            for(int i = 0; i< priorityScrapeApps.Count; i++)
            {
                // Doesn't seem to work. Look into it dumb CE
                // Edit: It does work without changes. Tired me forgot about caching of ComputerUtils
                if(priorityScrapeApps[i].minNextScrape <= DateTime.Now)
                {
                    priorityScrapeApps.RemoveAt(i);
                    i--;
                }
            }
            //Logger.Log("Length of priority scrape queue: " + priorityScrapeApps.Count, LoggingType.Important);
            if(priorityScrapeApps.FirstOrDefault(x => x.id == id) == null)
            {
                priorityScrapeApps.Add(new PriorityScrape { id = id, headset = headset, minNextScrape = DateTime.Now + new TimeSpan(0, 10, 0)});
                Logger.Log("added " + id + ", priority scrape length: " + priorityScrapeApps.Count, LoggingType.Important);
                ScrapeNext();
            }
        }

        public static void ScrapeNext()
        {
            for(int i = 0; i < priorityScrapeApps.Count; i++)
            {
                if(!priorityScrapeApps[i].scraped)
                {
                    priorityScrapeApps[i].scraped = true;
                    ScrapePriority(priorityScrapeApps[i]);
                    return;
                }
            }
        }

        public static void ScrapePriority(PriorityScrape s)
        {
            if (priorityScrapeRunning || scrapeResumeTime > DateTime.Now) return;
            priorityScrapeRunning = true;
            Logger.Log("Starting priority scrape of: " + s.id, LoggingType.Important);
            bool success = false;
            for (int i = 1; i <= 3 && !success; i++)
            {
                try
                {
                    Scrape(new ToScrapeApp(s.id, ""), s.headset, true);
                    success = true;
                }
                catch (Exception ex)
                {
                    if (i == 3)
                    {
                        Logger.Log("Scraping of id " + s.id + " failed. No retiries remaining. Next attempt to scrape in next scrape: " + ex.ToString(), LoggingType.Error);
                        priorityScrapeRunning = false;
                        if (Stop()) return;
                    }
                }
            }
            priorityScrapeRunning = false;
            ScrapeNext();
        }

        public static void StartScrapingThread()
        {
            if(config.mongoDBUrl == "")
            {
                Logger.Log("Cannot scrape as mongodb isn't set");
                return;
            }
            ScrapeAll();
        }

        public static void CheckRunning()
        {
            if(DateTime.Now - new TimeSpan(0, minutesPause + 10, 0) > lastUpdate)
            {
                // Time to restart scraping threads
                OculusDBServer.SendMasterWebhookMessage("Server restarting", "Scraping thread hasn't updated in the last 30 min. Restarting the server", 0xFFFF00);
                Updater.Restart(Path.GetFileName(Assembly.GetExecutingAssembly().Location), OculusDBEnvironment.workingDir);
            }
        }

        public static void ScrapeAll()
        {
            //GraphQLClient.log = true;
            if(config.ScrapingResumeData.currentScrapeStart == DateTime.MinValue)
            {
                config.ScrapingResumeData.currentScrapeStart = DateTime.Now;
                config.Save();
            }
            if(config.oculusTokens.Count <= 0)
            {
                Logger.Log("Cannot scrape as no Oculus tokens are configured", LoggingType.Error);
                OculusDBServer.SendMasterWebhookMessage("Cannot scrape data", "Please add 1 or more Oculus tokens to the config", 0xFF0000);
                return;
            }
            failedApps = 0;
            SwitchToken();
            OculusDBServer.SendMasterWebhookMessage("Info", "Scrape will be started now", 0x00FF00);
            config.ScrapingResumeData.appsToScrape = 0;
            SetupLimitedScrape(Headset.RIFT);
            SetupLimitedScrape(Headset.HOLLYWOOD);
            SetupLimitedScrape(Headset.GEARVR);
            SetupLimitedScrape(Headset.PACIFIC);
            SetupLimitedScrape(Headset.SEACLIFF);
            SetupLimitedScrapeAppLab();
        }

        public static bool IsTokenValidUserToken()
        {
            ViewerData<OculusUserWrapper> currentUser = GraphQLClient.GetCurrentUser();
            if (currentUser == null) return false;
            if (currentUser.data == null) return false;
            if (currentUser.data.viewer == null) return false;
            if (currentUser.data.viewer.user == null) return false;

            // Maybe change that to not include username
            Logger.Log("Using token of " + currentUser.data.viewer.user.alias);
            return true;
        }

        public static void SwitchToken()
        {
            config.lastOculusToken = (config.lastOculusToken + 1) % config.oculusTokens.Count;
            GraphQLClient.oculusStoreToken = config.oculusTokens[config.lastOculusToken];
            if(!IsTokenValidUserToken())
            {
                Logger.Log("Current token didn't return an username: " + config.lastOculusToken);
                OculusDBServer.SendMasterWebhookMessage("Token issue", "Token at index " + config.lastOculusToken + " didn't return an username. It is either expired or got rate limited", 0xFFFF00);
            } else
            {
                config.lastValidToken = config.lastOculusToken;
                try
                {
                    Logger.Log("Getting entitlements of token at " + config.lastOculusToken);
                    ViewerData<OculusUserWrapper> user = GraphQLClient.GetActiveEntitelments();
                    if(user == null || user.data == null || user.data.viewer == null || user.data.viewer.user == null || user.data.viewer.user.active_entitlements == null ||user.data.viewer.user.active_entitlements.nodes == null)
                    {
                        throw new Exception("Fetching of active entitlements failed");
                    }
                    userEntitlements = user.data.viewer.user.active_entitlements.nodes;
                    OculusDBServer.SendMasterWebhookMessage("Info", "Got entitlements for token at index " + config.lastOculusToken, 0x00FF00);
                } catch (Exception e)
                {
                    Logger.Log(e.ToString(), LoggingType.Warning);
                    OculusDBServer.SendMasterWebhookMessage("Fetching of active entitlements failed", "Failed for token at index " + config.lastOculusToken + ". This may result in some prices being showed as free while they're actually not free.", 0xFFFF00);
                }
            }
            config.Save();
        }

        public static void FinishCurrentScrape()
        {
            Logger.Log("Finished scrape of OculusDB");
            ///////////////////////////////
            // There are DB size issues. //
            ///////////////////////////////
            if(config.deleteOldData)
            {
                Logger.Log("Deleting old data");
                Logger.Log("Deleted " + (MongoDBInteractor.DeleteOldDataExceptVersions(config.ScrapingResumeData.currentScrapeStart, config.ScrapingResumeData.updated)) + " documents from data collection which are before " + config.ScrapingResumeData.currentScrapeStart, LoggingType.Important);
            }

            config.ScrapingResumeData.updated = new List<string>();
            config.Save();
            // Has been replaced by application specific activity sending
            //DiscordWebhookSender.SendActivity(config.ScrapingResumeData.currentScrapeStart);
            config.lastDBUpdate = config.ScrapingResumeData.currentScrapeStart;
            config.ScrapingResumeData.currentScrapeStart = DateTime.MinValue;
            config.Save();
            OculusDBServer.SendMasterWebhookMessage("Info", "Scrape which has been started on " + config.lastDBUpdate + " has finished. The next scrape will start " + (config.pauseAfterScrape ? "in " + minutesPause + " minutes" : "now"), 0x00FF00);
            if (config.pauseAfterScrape) Task.Delay(minutesPause * 60 * 1000).Wait();
            StartScrapingThread();
        }

        public static void SetupLimitedScrapeAppLab()
        {
            Thread t = new Thread(() =>
            {
                WebClient c = new WebClient();
                List<SidequestApplabGame> s = JsonSerializer.Deserialize<List<SidequestApplabGame>>(c.DownloadString("https://api.sidequestvr.com/v2/apps?limit=1000&is_app_lab=true&has_oculus_url=true&sortOn=downloads&descending=true"));
                List<ToScrapeApp> ids = new List<ToScrapeApp>();
                foreach (SidequestApplabGame a in s)
                {
                    string id = a.oculus_url.Replace("/?utm_source=sidequest", "").Replace("?utm_source=sq_pdp&utm_medium=sq_pdp&utm_campaign=sq_pdp&channel=sq_pdp", "").Replace("https://www.oculus.com/experiences/quest/", "").Replace("/", "");
                    if (id.Length <= 16)
                    {
                        config.ScrapingResumeData.appsToScrape++;
                        ids.Add(new ToScrapeApp(id, a.image_url));
                    }
                }
                int current = 0;
                while (current < ids.Count)
                {
                    Thread t = new Thread((ids) =>
                    {
                        List<ToScrapeApp> idds = (List<ToScrapeApp>)ids;
                        Logger.Log("Started Scraping thread for " + idds.Count + " apps of AppLab");
                        foreach (ToScrapeApp id in idds)
                        {
                            bool success = false;
                            for (int i = 1; i <= 3 && !success; i++)
                            {
                                try
                                {
                                    Scrape(id, Headset.HOLLYWOOD);
                                    success = true;
                                }
                                catch (Exception ex)
                                {
                                    if (i == 3)
                                    {
                                        Logger.Log("Scraping of id " + id.id + " failed. No retiries remaining. Next attempt to scrape in next scrape: " + ex.ToString(), LoggingType.Error);
                                        failedApps++;
                                        if (Stop()) return;
                                    }
                                    //else Logger.Log("Scraping of id " + id + " failed. Retrying. Remaining attempts: " + (3 - i), LoggingType.Warning);
                                }
                            }
                        }
                        doneScrapeThreads++;
                        Logger.Log("Scraping thread for " + idds.Count + " apps of AppLab has finished. Threads to finish: " + (totalScrapeThreads - doneScrapeThreads));
                        if (doneScrapeThreads == totalScrapeThreads) FinishCurrentScrape();
                    });
                    t.Start(ids.Skip(current).Take(maxAppsToDo).ToList());
                    current += maxAppsToDo;
                    totalScrapeThreads++;
                }
            });
            t.Start();
        }
        
        public static bool Stop()
        {
            if(failedApps == maxAppsToFail)
            {
                Logger.Log(maxAppsToFail + " apps failed to get scraped", LoggingType.Error);
                OculusDBServer.SendMasterWebhookMessage("Warning", "More than " + maxAppsToFail + " apps have failed to get scraped. Token will be switched and retry will be in " + minutesPause + " minutes", 0xFF0000);
                scrapeResumeTime = DateTime.Now + TimeSpan.FromMinutes(minutesPause);
                Task.Delay(minutesPause * 60 * 1000).Wait();
                OculusDBServer.SendMasterWebhookMessage("Info", "Scrape will be restarted now", 0x00FF00);
                Logger.Log("Scrape will be started now", LoggingType.Important);
                ScrapeAll();
                return true;
            } else if(failedApps > maxAppsToFail)
            {
                Logger.Log("Stopping thread");
                return true;
            }
            return false;
        }

        public static void SetupLimitedScrape(Headset h)
        {
            Thread t = new Thread(() =>
            {
                int current = 0;
                Logger.Log("Settings up scraping threads for " + HeadsetTools.GetHeadsetCodeName(h));
                List<ToScrapeApp> ids = new List<ToScrapeApp>();
                try
                {
                    foreach (Application a in OculusInteractor.EnumerateAllApplications(h))
                    {
                        ids.Add(new ToScrapeApp(a.id, a.cover_square_image.uri));
                        config.ScrapingResumeData.appsToScrape++;
                    }
                } catch(Exception e)
                {
                    Logger.Log(e.ToString(), LoggingType.Warning);
                    //OculusDBServer.SendMasterWebhookMessage(e.Message, OculusDBServer.FormatException(e), 0xFF0000);
                }
                Logger.Log("Queued " + ids.Count + " apps for scraping for " + HeadsetTools.GetHeadsetCodeName(h));
                while (current < ids.Count)
                {
                    Thread t = new Thread((ids) =>
                    {
                        List<ToScrapeApp> idds = (List<ToScrapeApp>)ids;
                        Logger.Log("Started Scraping thread for " + idds.Count + " apps of " + HeadsetTools.GetHeadsetCodeName(h));
                        foreach (ToScrapeApp id in idds)
                        {
                            bool success = false;
                            for(int i = 1; i <= 3 && !success; i++)
                            {
                                try
                                {
                                    Scrape(id, h);
                                    success = true;
                                }
                                catch (Exception e)
                                {
                                    if (i == 3)
                                    {
                                        Logger.Log("Scraping of id " + id.id + " failed. No retiries remaining. Next attempt to scrape in next scrape:\n" + e.ToString(), LoggingType.Error);
                                        failedApps++;
                                        if (Stop()) return;
                                    }
                                    //else Logger.Log("Scraping of id " + id + " failed. Retrying. Remaining attempts: " + (3 - i), LoggingType.Warning);
                                }
                                config.ScrapingResumeData.scrapedApps++;
                            }
                        }
                        doneScrapeThreads++;
                        Logger.Log("Scraping thread for " + idds.Count + " apps of " + HeadsetTools.GetHeadsetCodeName(h) + " has finished. Threads to finish: " + (totalScrapeThreads - doneScrapeThreads));
                        if (doneScrapeThreads == totalScrapeThreads) FinishCurrentScrape();
                    });
                    t.Start(ids.Skip(current).Take(maxAppsToDo).ToList());
                    current += maxAppsToDo;
                    totalScrapeThreads++;
                }
            });
            t.Start();
        }

        public static UserEntitlement GetEntitlementStatusOfAppOrDLC(string appId, string dlcId = null, string dlcName = "")
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

        public static IAPEntitlement GetEntitlementOfDLC(string appId, string dlcId)
        {
            if (userEntitlements.Count <= 0) return null;
            foreach(Entitlement entitlement in userEntitlements)
            {
                if(entitlement.item.id == appId)
                {
                    foreach(IAPEntitlement dlc in entitlement.item.active_dlc_entitlements)
                    {
                        if(dlc.item.id == dlcId)
                        {
                            return dlc;
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        public static void DownloadImage(DBApplication a)
        {
            try
            {
                string loc = OculusDBEnvironment.dataDir + "images" + Path.DirectorySeparatorChar + a.id + Path.GetExtension(a.img.Split('?')[0]);
                if (a.img == "") return;
                WebClient c = new WebClient();
                c.Headers.Add("user-agent", OculusDBEnvironment.userAgent);
                c.DownloadFile(a.img, loc);
                if (!loc.EndsWith(".webp"))
                {
                    try
                    {
                        using (var img = Image.Load(loc))
                        {
                            img.Save(OculusDBEnvironment.dataDir + "images" + Path.DirectorySeparatorChar + a.id + ".webp");
                        }
                        File.Delete(loc);
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Couldn't convert image to webp:\n" + e.ToString(), LoggingType.Warning);
                    }
                }
            } catch(Exception e)
            {
                Logger.Log("Couldn't download image of " + a.id + ":\n" + e.ToString, LoggingType.Warning);
            }
        }


        public static void Scrape(ToScrapeApp id, Headset headset, bool priority = false)
        {
            lastUpdate = DateTime.Now;
            if (MongoDBInteractor.DoesIdExistInCurrentScrape(id.id) && !priority)
            {
                //Logger.Log(id + " exists in current scrape. Skipping");
                return;
            }
            List<BsonDocument> activitiesToSend = new List<BsonDocument>();
            Logger.Log("Scraping " + id.id, LoggingType.Important);
            DateTime priorityScrapeStart = DateTime.Now;
            Application a = GraphQLClient.GetAppDetail(id.id, headset).data.node;
            if (a == null) throw new Exception("Application is null");
            if (MongoDBInteractor.GetLastEventWithIDInDatabase(a.id) == null)
            {
                DBActivityNewApplication e = new DBActivityNewApplication();
                e.id = a.id;
                e.hmd = headset;
                e.publisherName = a.publisher_name;
                e.displayName = a.displayName;
                if(a.baseline_offer != null) e.priceOffset = a.baseline_offer.price.offset_amount;
                if (a.current_offer != null && a.current_offer.price != null) e.priceFormatted = FormatPrice(e.priceOffsetNumerical, a.current_offer.price.currency);
                e.displayLongDescription = a.display_long_description;
                e.releaseDate = TimeConverter.UnixTimeStampToDateTime((long)a.release_date);
                e.supportedHmdPlatforms = a.supported_hmd_platforms;
                activitiesToSend.Add(e.ToBsonDocument());
                MongoDBInteractor.AddBsonDocumentToActivityCollection(e.ToBsonDocument());
            }
            Data<Application> d = GraphQLClient.GetDLCs(a.id);
            string packageName = "";
            ConnectedList connected = MongoDBInteractor.GetConnected(a.id);
            List<string> updatedVersions = new List<string>();
            foreach (AndroidBinary b in GraphQLClient.AllVersionsOfApp(a.id).data.node.primary_binaries.nodes)
            {
                if(packageName == "")
                {
                    PlainData<AppBinaryInfoContainer> info = GraphQLClient.GetAssetFiles(a.id, b.versionCode);
                    packageName = info.data.app_binary_info.info[0].binary.package_name;
                }
                if(b != null && priority)
                {
                    Logger.Log("Scraping v " + b.version, LoggingType.Important);
                }
                AndroidBinary bin = priority ? GraphQLClient.GetBinaryDetails(b.id).data.node : b;
                if (bin == null) continue; // skip if version was unable to be fetched
                // Preserve changelogs and obbs across scrapes by:
                // - Don't delete versions after scrape
                // - If not priority scrape enter changelog and obb of most recent versions
                if(!priority && connected.versions.FirstOrDefault(x => x.id == bin.id) != null)
                {
                    bin.changeLog = connected.versions.FirstOrDefault(x => x.id == bin.id).changeLog;
                }

                MongoDBInteractor.AddVersion(bin, a, headset, priority ? null : connected.versions.FirstOrDefault(x => x.id == bin.id));
                updatedVersions.Add(bin.id);
                BsonDocument lastActivity = MongoDBInteractor.GetLastEventWithIDInDatabase(b.id);
                    
                DBActivityNewVersion newVersion = new DBActivityNewVersion();
                newVersion.id = bin.id;
                newVersion.changeLog = bin.changeLog;
                newVersion.parentApplication.id = a.id;
                newVersion.parentApplication.hmd = headset;
                newVersion.parentApplication.canonicalName = a.canonicalName;
                newVersion.parentApplication.displayName = a.displayName;
                newVersion.releaseChannels = bin.binary_release_channels.nodes;
                newVersion.version = bin.version;
                newVersion.versionCode = bin.versionCode;
                newVersion.uploadedTime = TimeConverter.UnixTimeStampToDateTime(bin.created_date);
                if (lastActivity == null)
                {
                    activitiesToSend.Add(newVersion.ToBsonDocument());
                    MongoDBInteractor.AddBsonDocumentToActivityCollection(newVersion.ToBsonDocument());
                }
                else
                {
                    DBActivityVersionUpdated oldUpdate = lastActivity["__OculusDBType"] == DBDataTypes.ActivityNewVersion ? ObjectConverter.ConvertCopy<DBActivityVersionUpdated, DBActivityNewVersion>(ObjectConverter.ConvertToDBType(lastActivity)) : ObjectConverter.ConvertToDBType(lastActivity);
                    if (oldUpdate.changeLog != newVersion.changeLog || String.Join(',', oldUpdate.releaseChannels.Select(x => x.channel_name).ToList()) != String.Join(',', newVersion.releaseChannels.Select(x => x.channel_name).ToList()))
                    {
                        DBActivityVersionUpdated toAdd = ObjectConverter.ConvertCopy<DBActivityVersionUpdated, DBActivityNewVersion>(newVersion);
                        toAdd.__OculusDBType = DBDataTypes.ActivityVersionUpdated;
                        toAdd.__lastEntry = lastActivity["_id"].ToString();
                        activitiesToSend.Add(toAdd.ToBsonDocument());
                        MongoDBInteractor.AddBsonDocumentToActivityCollection(toAdd.ToBsonDocument());
                    }
                }
            }

            MongoDBInteractor.AddApplication(a, headset, id.image, packageName);
            MongoDBInteractor.DeleteOldVersions(priorityScrapeStart, a.id, updatedVersions);
            if (d.data.node.latest_supported_binary != null && d.data.node.latest_supported_binary.firstIapItems != null)
            {
                foreach (Node<AppItemBundle> dlc in d.data.node.latest_supported_binary.firstIapItems.edges)
                {
                    // For whatever reason Oculus sets parentApplication wrong. e. g. Beat Saber for Rift: it sets Beat Saber for quest
                    dlc.node.parentApplication.canonicalName = a.canonicalName;
                    dlc.node.parentApplication.id = a.id;
                    DBActivityNewDLC newDLC = new DBActivityNewDLC();
                    newDLC.id = dlc.node.id;
                    newDLC.parentApplication.id = a.id;
                    newDLC.parentApplication.hmd = headset;
                    newDLC.parentApplication.canonicalName = a.canonicalName;
                    newDLC.parentApplication.displayName = a.displayName;
                    newDLC.displayName = dlc.node.display_name;
                    newDLC.displayShortDescription = dlc.node.display_short_description;
                    newDLC.latestAssetFileId = dlc.node.latest_supported_asset_file != null ? dlc.node.latest_supported_asset_file.id : "";

                    // Give me one reason why Oculus returns a different id in the entitlement request and dlc request for the mosterpack dlc????
                    UserEntitlement ownsDlc = GetEntitlementStatusOfAppOrDLC(a.id, dlc.node.id, dlc.node.display_name);

                    newDLC.priceOffset = dlc.node.current_offer.price.offset_amount;
                    if (ownsDlc == UserEntitlement.FAILED && newDLC.priceOffsetNumerical <= 0 || ownsDlc == UserEntitlement.OWNED && newDLC.priceOffsetNumerical <= 0) continue;

                    newDLC.priceFormatted = FormatPrice(newDLC.priceOffsetNumerical, a.current_offer.price.currency);
                    BsonDocument oldDLC = MongoDBInteractor.GetLastEventWithIDInDatabase(dlc.node.id);
                    if (dlc.node.IsIAPItem())
                    {
                        MongoDBInteractor.AddDLC(dlc.node, headset);
                        if (oldDLC == null)
                        {
                            activitiesToSend.Add(newDLC.ToBsonDocument());
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(newDLC.ToBsonDocument());
                        }
                        else if (oldDLC["priceOffset"] != newDLC.priceOffset || oldDLC["displayName"] != newDLC.displayName || oldDLC["displayShortDescription"] != newDLC.displayShortDescription)
                        {
                            DBActivityDLCUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCUpdated, DBActivityNewDLC>(newDLC);
                            updated.__lastEntry = oldDLC["_id"].ToString();
                            updated.__OculusDBType = DBDataTypes.ActivityDLCUpdated;
                            activitiesToSend.Add(updated.ToBsonDocument());
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(updated.ToBsonDocument());
                        }

                    }
                    else
                    {
                        MongoDBInteractor.AddDLCPack(dlc.node, headset, a);
                        DBActivityNewDLCPack newDLCPack = ObjectConverter.ConvertCopy<DBActivityNewDLCPack, DBActivityNewDLC>(newDLC);
                        newDLCPack.__OculusDBType = DBDataTypes.ActivityNewDLCPack;
                        foreach (Node<IAPItem> item in dlc.node.bundle_items.edges)
                        {
                            Node<AppItemBundle> matching = d.data.node.latest_supported_binary.firstIapItems.edges.FirstOrDefault(x => x.node.id == item.node.id);
                            if (matching == null) continue;
                            DBActivityNewDLCPackDLC dlcItem = new DBActivityNewDLCPackDLC();
                            dlcItem.id = matching.node.id;
                            dlcItem.displayName = matching.node.display_name;
                            dlcItem.displayShortDescription = matching.node.display_short_description;
                            newDLCPack.includedDLCs.Add(dlcItem);
                        }
                        if (oldDLC == null)
                        {
                            activitiesToSend.Add(newDLCPack.ToBsonDocument());
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(newDLCPack.ToBsonDocument());
                        }
                        else if (oldDLC["priceOffset"] != newDLCPack.priceOffset || oldDLC["displayName"] != newDLC.displayName || oldDLC["displayShortDescription"] != newDLC.displayShortDescription || String.Join(',', BsonSerializer.Deserialize<DBActivityNewDLCPack>(oldDLC).includedDLCs.Select(x => x.id).ToList()) != String.Join(',', newDLCPack.includedDLCs.Select(x => x.id).ToList()))
                        {
                            DBActivityDLCPackUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCPackUpdated, DBActivityNewDLCPack>(newDLCPack);
                            updated.__lastEntry = oldDLC["_id"].ToString();
                            updated.__OculusDBType = DBDataTypes.ActivityDLCPackUpdated;
                            activitiesToSend.Add(updated.ToBsonDocument());
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(updated.ToBsonDocument());
                        }
                    }
                }
            }
            DBActivityPriceChanged lastPriceChange = ObjectConverter.ConvertToDBType(MongoDBInteractor.GetLastPriceChangeOfApp(a.id));
            DBActivityPriceChanged priceChange = new DBActivityPriceChanged();
            priceChange.parentApplication.id = a.id;
            priceChange.parentApplication.hmd = headset;
            priceChange.parentApplication.canonicalName = a.canonicalName;
            priceChange.parentApplication.displayName = a.displayName;


            if (a.current_offer != null) priceChange.newPriceOffset = a.current_offer.price.offset_amount;
            

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
                        priceChange.newPriceOffset = a.baseline_offer.price.offset_amount;
                    }
                }
            }
            else if (ownsApp == UserEntitlement.OWNED)
            {
                if (a.baseline_offer != null) priceChange.newPriceOffset = a.baseline_offer.price.offset_amount;
            }


            if (a.current_offer != null) priceChange.newPriceFormatted = FormatPrice(priceChange.newPriceOffsetNumerical, a.current_offer.price.currency);
            if (lastPriceChange != null)
            {
                if (lastPriceChange.newPriceOffset != priceChange.newPriceOffset)
                {
                    priceChange.oldPriceFormatted = lastPriceChange.newPriceFormatted;
                    priceChange.oldPriceOffset = lastPriceChange.newPriceOffset;
                    priceChange.__lastEntry = lastPriceChange.__id;
                    activitiesToSend.Add(priceChange.ToBsonDocument());
                    MongoDBInteractor.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument());
                }
            }
            else
            {
                activitiesToSend.Add(priceChange.ToBsonDocument());
                MongoDBInteractor.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument());
            }
            if(priority)
            {
                MongoDBInteractor.DeleteOldData(priorityScrapeStart, new List<string> { a.id });
            }
            if(!priority) config.ScrapingResumeData.updated.Add(a.id);
            DiscordWebhookSender.SendActivity(activitiesToSend);
            Logger.Log("Scraped " + id.id);
            config.Save();
        }

        public static string FormatPrice(long offsetAmount, string currency)
        {
            string symbol = "";
            if (currency == "USD") symbol = "$";
            if (currency == "EUR") symbol = "€";
            string price = symbol + String.Format("{0:0.00}", offsetAmount / 100.0);
            
            return price;
        }
    }

    public class PriorityScrape
    {
        public bool scraped = false;
        public string id { get; set; } = "";
        public Headset headset { get; set; } = Headset.HOLLYWOOD;
        public DateTime minNextScrape { get; set; } = DateTime.Now;
    }

    public class SidequestApplabGame
    {
        public string oculus_url { get; set; } = "";
        public string name { get; set; } = "";
        public string image_url { get; set; } = "";
    }

    public class ToScrapeApp
    {
        public string id { get; set; } = "";
        public string image { get; set; } = "";

        public ToScrapeApp(string id, string image)
        {
            this.id = id;
            this.image = image;
        }
    }

    public enum UserEntitlement
    {
        FAILED,
        OWNED,
        NOTOWNED
    }
}
