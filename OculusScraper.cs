using ComputerUtils.Logging;
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusDB
{
    public class OculusScraper
    {
        public static Config config { get
            {
                return OculusDBEnvironment.config;
            } }
        public static int totalScrapeThreads = 0;
        public static int doneScrapeThreads = 0;

        public static void StartScrapingThread()
        {
            if(config.mongoDBUrl == "")
            {
                Logger.Log("Cannot scrape as mongodb isn't set");
                return;
            }
            ScrapeAll();
        }

        public static void ScrapeAll()
        {
            if(config.ScrapingResumeData.currentScrapeStart == DateTime.MinValue)
            {
                config.ScrapingResumeData.currentScrapeStart = DateTime.Now;
                config.Save();
            }

            SetupLimitedScrape(Headset.RIFT);
            SetupLimitedScrape(Headset.MONTEREY);
            SetupLimitedScrape(Headset.GEARVR);
            SetupLimitedScrape(Headset.PACIFIC);
            SetupLimitedScrapeAppLab();
        }

        public static void FinishCurrentScrape()
        {
            Logger.Log("Finished scrape of OculusDB");
            ////////////////////////////////////////////////////////////////
            // If there are db size issues move this below Config.Save(); //
            ////////////////////////////////////////////////////////////////
            if(config.deleteOldData)
            {
                Logger.Log("Deleting old data");
                Logger.Log("Deleted " + (MongoDBInteractor.DeleteOldData(config.lastDBUpdate)) + " documents from data collection which are before " + config.lastDBUpdate, LoggingType.Important);
            }
            DiscordWebhookSender.SendActivity(config.ScrapingResumeData.currentScrapeStart);
            config.lastDBUpdate = config.ScrapingResumeData.currentScrapeStart;
            config.ScrapingResumeData.currentScrapeStart = DateTime.MinValue;
            config.Save();
            StartScrapingThread();
        }

        public static void SetupLimitedScrapeAppLab()
        {
            Thread t = new Thread(() =>
            {
                WebClient c = new WebClient();
                List<SidequestApplabGame> s = JsonSerializer.Deserialize<List<SidequestApplabGame>>(c.DownloadString("https://api.sidequestvr.com/v2/apps?limit=1000&is_app_lab=true&has_oculus_url=true&sortOn=downloads&descending=true"));
                List<string> ids = new List<string>();
                foreach (SidequestApplabGame a in s)
                {
                    string id = a.oculus_url.Replace("/?utm_source=sidequest", "").Replace("?utm_source=sq_pdp&utm_medium=sq_pdp&utm_campaign=sq_pdp&channel=sq_pdp", "").Replace("https://www.oculus.com/experiences/quest/", "").Replace("/", "");
                    if (id.Length <= 16)
                    {
                        ids.Add(id);
                    }
                }
                int current = 0;
                const int maxAppsToDo = 500;
                while (current < ids.Count)
                {
                    Thread t = new Thread((ids) =>
                    {
                        List<string> idds = (List<string>)ids;
                        Logger.Log("Started Scraping thread for " + idds.Count + " apps of AppLab");
                        foreach (string id in idds)
                        {
                            bool success = false;
                            for (int i = 1; i <= 3 && !success; i++)
                            {
                                try
                                {
                                    Scrape(id, Headset.MONTEREY);
                                    success = true;
                                }
                                catch (Exception ex)
                                {
                                    if (i == 3) Logger.Log("Scraping of id " + id + " failed. No retiries remaining. Next attempt to scrape in next scrape.", LoggingType.Error);
                                    else Logger.Log("Scraping of id " + id + " failed. Retrying. Remaining attempts: " + (3 - i), LoggingType.Warning);
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

        public static void SetupLimitedScrape(Headset h)
        {
            Thread t = new Thread(() =>
            {
                int current = 0;
                const int maxAppsToDo = 500;
                Logger.Log("Settings up scraping threads for " + HeadsetTools.GetHeadsetCodeName(h));
                List<string> ids = new List<string>();
                try
                {
                    foreach (Application a in OculusInteractor.EnumerateAllApplications(h))
                    {
                        ids.Add(a.id);
                    }
                } catch(Exception e)
                {
                    OculusDBServer.SendMasterWebhookMessage(e.Message, OculusDBServer.FormatException(e), 0xFF0000);
                }
                
                while (current < ids.Count)
                {
                    Thread t = new Thread((ids) =>
                    {
                        List<string> idds = (List<string>)ids;
                        Logger.Log("Started Scraping thread for " + idds.Count + " apps of " + HeadsetTools.GetHeadsetCodeName(h));
                        foreach (string id in idds)
                        {
                            bool success = false;
                            for(int i = 1; i <= 3 && !success; i++)
                            {
                                try
                                {
                                    Scrape(id, h);
                                    success = true;
                                }
                                catch (Exception ex)
                                {
                                    if (i == 3) Logger.Log("Scraping of id " + id + " failed. No retiries remaining. Next attempt to scrape in next scrape.", LoggingType.Error);
                                    else Logger.Log("Scraping of id " + id + " failed. Retrying. Remaining attempts: " + (3 - i), LoggingType.Warning);
                                }
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


        public static void Scrape(string id, Headset headset)
        {
            if (MongoDBInteractor.DoesIdExistInCurrentScrape(id))
            {
                Logger.Log(id + " exists in current scrape. Skipping");
                return;
            }
            Application a = GraphQLClient.GetAppDetail(id, headset).data.node;
            if (MongoDBInteractor.GetLastEventWithIDInDatabase(a.id) == null)
            {
                DBActivityNewApplication e = new DBActivityNewApplication();
                e.id = a.id;
                e.hmd = headset;
                e.publisherName = a.publisher_name;
                e.displayName = a.displayName;
                e.priceFormatted = a.current_offer.price.formatted;
                e.priceOffset = a.current_offer.price.offset_amount;
                e.displayLongDescription = a.display_long_description;
                e.releaseDate = TimeConverter.UnixTimeStampToDateTime((long)a.release_date);
                e.supportedHmdPlatforms = a.supported_hmd_platforms;
                MongoDBInteractor.AddBsonDocumentToActivityCollection(e.ToBsonDocument());
            }
            Data<Application> d = GraphQLClient.GetDLCs(a.id);
            foreach (AndroidBinary b in GraphQLClient.AllVersionsOfApp(a.id).data.node.primary_binaries.nodes)
            {
                MongoDBInteractor.AddVersion(b, a, headset);
                BsonDocument lastActivity = MongoDBInteractor.GetLastEventWithIDInDatabase(b.id);
                    
                DBActivityNewVersion newVersion = new DBActivityNewVersion();
                newVersion.id = b.id;
                newVersion.parentApplication.id = a.id;
                newVersion.parentApplication.hmd = headset;
                newVersion.parentApplication.canonicalName = a.canonicalName;
                newVersion.parentApplication.displayName = a.displayName;
                newVersion.releaseChannels = b.binary_release_channels.nodes;
                newVersion.version = b.version;
                newVersion.versionCode = b.versionCode;
                newVersion.uploadedTime = TimeConverter.UnixTimeStampToDateTime(b.created_date);
                if (lastActivity == null)
                {
                    MongoDBInteractor.AddBsonDocumentToActivityCollection(newVersion.ToBsonDocument());
                }
                else
                {
                    DBActivityVersionUpdated oldUpdate = lastActivity["__OculusDBType"] == DBDataTypes.ActivityNewVersion ? ObjectConverter.ConvertCopy<DBActivityVersionUpdated, DBActivityNewVersion>(ObjectConverter.ConvertToDBType(lastActivity)) : ObjectConverter.ConvertToDBType(lastActivity);
                    if (String.Join(',', oldUpdate.releaseChannels.Select(x => x.channel_name).ToList()) != String.Join(',', newVersion.releaseChannels.Select(x => x.channel_name).ToList()))
                    {
                        DBActivityVersionUpdated toAdd = ObjectConverter.ConvertCopy<DBActivityVersionUpdated, DBActivityNewVersion>(newVersion);
                        toAdd.__OculusDBType = DBDataTypes.ActivityVersionUpdated;
                        toAdd.__lastEntry = lastActivity["_id"].ToString();
                        MongoDBInteractor.AddBsonDocumentToActivityCollection(toAdd.ToBsonDocument());
                    }
                }
            }
            if (d.data.node.latest_supported_binary.firstIapItems != null)
            {
                Logger.Log("Adding " + d.data.node.latest_supported_binary.firstIapItems.edges.Count + " of " + d.data.node.latest_supported_binary.firstIapItems.count + " DLCs");
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
                    newDLC.priceFormatted = dlc.node.current_offer.price.formatted;
                    newDLC.priceOffset = dlc.node.current_offer.price.offset_amount;
                    BsonDocument oldDLC = MongoDBInteractor.GetLastEventWithIDInDatabase(dlc.node.id);
                    if (dlc.node.IsIAPItem())
                    {
                        MongoDBInteractor.AddDLC(dlc.node, headset);
                        if (oldDLC == null)
                        {
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(newDLC.ToBsonDocument());
                        }
                        else if (oldDLC["priceOffset"] != newDLC.priceOffset || oldDLC["displayName"] != newDLC.displayName || oldDLC["displayShortDescription"] != newDLC.displayShortDescription)
                        {
                            DBActivityDLCUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCUpdated, DBActivityNewDLC>(newDLC);
                            updated.__lastEntry = oldDLC["_id"].ToString();
                            updated.__OculusDBType = DBDataTypes.ActivityDLCUpdated;
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(updated.ToBsonDocument().ToBsonDocument().ToBsonDocument().ToBsonDocument());
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
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(newDLCPack.ToBsonDocument());
                        }
                        else if (oldDLC["priceOffset"] != newDLCPack.priceOffset || oldDLC["displayName"] != newDLC.displayName || oldDLC["displayShortDescription"] != newDLC.displayShortDescription || String.Join(',', BsonSerializer.Deserialize<DBActivityNewDLCPack>(oldDLC).includedDLCs.Select(x => x.id).ToList()) != String.Join(',', newDLCPack.includedDLCs.Select(x => x.id).ToList()))
                        {
                            DBActivityDLCPackUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCPackUpdated, DBActivityNewDLCPack>(newDLCPack);
                            updated.__lastEntry = oldDLC["_id"].ToString();
                            updated.__OculusDBType = DBDataTypes.ActivityDLCPackUpdated;
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(updated.ToBsonDocument());
                        }
                    }
                }
            }
                
            MongoDBInteractor.AddApplication(a, headset);
            DBActivityPriceChanged lastPriceChange = ObjectConverter.ConvertToDBType(MongoDBInteractor.GetLastPriceChangeOfApp(a.id));
            DBActivityPriceChanged priceChange = new DBActivityPriceChanged();
            priceChange.parentApplication.id = a.id;
            priceChange.parentApplication.hmd = headset;
            priceChange.parentApplication.canonicalName = a.canonicalName;
            priceChange.parentApplication.displayName = a.displayName;
            priceChange.newPriceFormatted = a.current_offer.price.formatted;
            priceChange.newPriceOffset = a.current_offer.price.offset_amount;
            if (lastPriceChange != null)
            {
                if (lastPriceChange.newPriceOffset != a.current_offer.price.offset_amount)
                {
                    priceChange.oldPriceFormatted = lastPriceChange.newPriceFormatted;
                    priceChange.oldPriceOffset = lastPriceChange.newPriceOffset;
                    priceChange.__lastEntry = lastPriceChange.__id;
                    MongoDBInteractor.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument());
                }
            }
            else MongoDBInteractor.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument());
        }
    }

    public class SidequestApplabGame
    {
        public string oculus_url { get; set; } = "";
        public string name { get; set; } = "";
        public string image_url { get; set; } = "";
    }
}
