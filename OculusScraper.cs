using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB
{
    public class OculusScraper
    {
        public static Config config { get
            {
                return OculusDBEnvironment.config;
            } }

        public static void StartScrapingThread()
        {
            if(config.mongoDBUrl == "")
            {
                Logger.Log("Cannot scrape as mongodb isn't set");
                return;
            }
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    ScrapeAll();
                }
            });
            t.Start();
        }

        public static void ScrapeAll()
        {
            if(config.ScrapingResumeData.currentScrapeStart == DateTime.MinValue)
            {
                config.ScrapingResumeData.currentScrapeStart = DateTime.Now;
                config.Save();
            }

            Scrape(Headset.RIFT);
            Scrape(Headset.MONTEREY);
            Scrape(Headset.GEARVR);
            Scrape(Headset.PACIFIC);

            config.lastDBUpdate = config.ScrapingResumeData.currentScrapeStart;
            config.ScrapingResumeData.currentScrapeStart = DateTime.MinValue;
            config.Save();
        }


        public static void Scrape(Headset headset)
        {
            bool skip = config.ScrapingResumeData.currentAppId != "";
            foreach (Application a in OculusInteractor.EnumerateAllApplicationsDetail(headset))
            {
                if (MongoDBInteractor.GetLastEventWithIDInDatabase(a.id) == null)
                {
                    DBActivityNewApplication e = new DBActivityNewApplication();
                    e.id = a.id;
                    e.hmd = headset;
                    e.publisher_name = a.publisher_name;
                    e.displayName = a.displayName;
                    e.priceFormatted = a.current_offer.price.formatted;
                    e.priceOffset = a.current_offer.price.offset_amount;
                    e.displayLongDescription = a.display_long_description;
                    e.releaseDate = TimeConverter.UnixTimeStampToDateTime((long)a.release_date);
                    e.supportedHmdPlatforms = a.supported_hmd_platforms;
                    MongoDBInteractor.AddBsonDocumentToActivityCollection(e.ToBsonDocument());
                }
                DBActivityPriceChanged lastPriceChange = ObjectConverter.ConvertToDBType(MongoDBInteractor.GetLastPriceChangeOfApp(a.id));
                DBActivityPriceChanged priceChange = new DBActivityPriceChanged();
                priceChange.parentApplication.id = a.id;
                priceChange.parentApplication.hmd = headset;
                priceChange.parentApplication.canonicalName = a.canonicalName;
                priceChange.parentApplication.displayName = a.displayName;
                //priceChange.newPriceFormatted = a.current_offer.price.formatted;
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
                            MongoDBInteractor.AddDLCPack(dlc.node, headset);
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
            }
        }
    }
}
