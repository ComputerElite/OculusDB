using ComputerUtils.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using OculusDB.Analytics;
using OculusDB.Database;
using OculusDB.QAVS;
using OculusDB.Users;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MongoDB.Bson.IO;
using OculusDB.ScrapingMaster;

namespace OculusDB
{
    public class MongoDBInteractor
    {
        public static MongoClient? mongoClient;
        public static IMongoDatabase? oculusDBDatabase;
        public static IMongoCollection<BsonDocument>? dataCollection;
        public static IMongoCollection<BsonDocument>? activityCollection;
        public static IMongoCollection<ActivityWebhook>? webhookCollection;
        public static IMongoCollection<Analytic>? analyticsCollection;
        public static IMongoCollection<AppToScrape>? appsToScrape;
		public static IMongoCollection<VersionAlias>? versionAliases;
        
        public static IMongoCollection<DBApplication>? blockedApps;
        
        public static IMongoCollection<DBApplication>? applicationCollection;
        public static IMongoCollection<DBAppImage>? appImages;
        public static IMongoCollection<DBIAPItem>? dlcCollection;
        public static IMongoCollection<DBIAPItemPack>? dlcPackCollection;
        public static IMongoCollection<DBVersion>? versionsCollection;

		public static IMongoCollection<QAVSReport>? qAVSReports;
        
        
        public static List<string> blockedAppsCache = new List<string>();

        public static void MigrateFromDataCollectionToOtherCollections()
        {
            long total = dataCollection.CountDocuments(x => true);
            int i = 0;
            const int count = 10000;
            List<BsonDocument> docs = dataCollection.Find(x => true).Skip(i).Limit(count).ToList();
            while ((docs = dataCollection.Find(x => true).Skip(i).Limit(count).ToList()).Count > 0)
            {
                i += count;
                List<DBApplication> apps = new();
                List<DBVersion> versions = new();
                List<DBIAPItem> dlcs = new();
                List<DBIAPItemPack> packs = new();
                Logger.Log("Migrating " + i + " / " + total);
                foreach (BsonDocument d in docs)
                {
                    switch (d["__OculusDBType"].AsString)
                    {
                        case DBDataTypes.Version:
                            DBVersion v = ObjectConverter.ConvertToDBType(d);
                            versions.Add(v);
                            continue;
                        case DBDataTypes.Application:
                            DBApplication a = ObjectConverter.ConvertToDBType(d);
                            apps.Add(a);
                            continue;
                        case DBDataTypes.IAPItem:
                            DBIAPItem dlc = ObjectConverter.ConvertToDBType(d);
                            dlcs.Add(dlc);
                            continue;
                        case DBDataTypes.IAPItemPack:
                            DBIAPItemPack dlcPack = ObjectConverter.ConvertToDBType(d);
                            packs.Add(dlcPack);
                            continue;
                    }
                }
                if(apps.Count > 0) applicationCollection.InsertMany(apps);
                if(versions.Count > 0) versionsCollection.InsertMany(versions);
                if(dlcs.Count > 0) dlcCollection.InsertMany(dlcs);
                if(packs.Count > 0) dlcPackCollection.InsertMany(packs);
            }
        }
        
        public static void Initialize()
        {
            BsonChunkPool.Default = new BsonChunkPool(512, 1024 * 64);
            ConventionPack pack = new ConventionPack();
            pack.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("Ignore extra elements cause it's annoying", pack, t => true);

            // Don't ask. It's important stuff to reduce DB size and fix a lot of errors
            RemoveIdRemap<Application>();
            RemoveIdRemap<ParentApplication>();
            RemoveIdRemap<AndroidBinary>();
            RemoveIdRemap<AppStoreOffer>();
            RemoveIdRemap<DBVersion>();
            RemoveIdRemap<DBActivityNewApplication>();
            RemoveIdRemap<DBActivityApplicationUpdated>();
            RemoveIdRemap<DBActivityNewVersion>();
            RemoveIdRemap<DBActivityVersionUpdated>();
            RemoveIdRemap<DBActivityPriceChanged>();
            RemoveIdRemap<DBActivityNewDLC>();
            RemoveIdRemap<DBActivityNewDLCPack>();
            RemoveIdRemap<DBActivityNewDLCPackDLC>();
            RemoveIdRemap<DBActivityDLCUpdated>();
            RemoveIdRemap<DBActivityDLCPackUpdated>();
			RemoveIdRemap<DBActivityVersionChangelogAvailable>();
			RemoveIdRemap<DBActivityVersionChangelogUpdated>();
			RemoveIdRemap<DBReleaseChannel>();
            RemoveIdRemap<DBApplication>();
            RemoveIdRemap<DBIAPItem>();

            BsonClassMap.RegisterClassMap<ReleaseChannel>(cm =>
            {
                cm.AutoMap();
                cm.UnmapProperty(x => x.latest_supported_binary); // Remove AndroidBinary
            });
            BsonClassMap.RegisterClassMap<IAPItem>(cm =>
            {
                cm.AutoMap();
                cm.UnmapProperty(x => x.parent_application);
                cm.UnmapProperty(x => x.latest_supported_asset_file);
                cm.UnmapProperty(x => x.id);
                cm.MapMember(x => x.id)
                    .SetElementName("id")
                    .SetOrder(0) //specific to your needs
                    .SetIsRequired(true); // again specific to your needs
            });
            
            mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
            oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
            dataCollection = oculusDBDatabase.GetCollection<BsonDocument>("data");
            webhookCollection = oculusDBDatabase.GetCollection<ActivityWebhook>("webhooks");
            activityCollection = oculusDBDatabase.GetCollection<BsonDocument>("activity");
            analyticsCollection = oculusDBDatabase.GetCollection<Analytic>("analytics");
            
            versionsCollection = oculusDBDatabase.GetCollection<DBVersion>("versions");
            applicationCollection = oculusDBDatabase.GetCollection<DBApplication>("apps");
            dlcCollection = oculusDBDatabase.GetCollection<DBIAPItem>("dlcs");
            dlcPackCollection = oculusDBDatabase.GetCollection<DBIAPItemPack>("dlcPacks");
            appImages = oculusDBDatabase.GetCollection<DBAppImage>("appImages");

            appsToScrape = oculusDBDatabase.GetCollection<AppToScrape>("appsToScrape");
            qAVSReports = oculusDBDatabase.GetCollection<QAVSReport>("QAVSReports");
            versionAliases = oculusDBDatabase.GetCollection<VersionAlias>("versionAliases");
            blockedApps = oculusDBDatabase.GetCollection<DBApplication>("blockedApps");

            UpdateBlockedAppsCache();
        }

        public static void UpdateBlockedAppsCache()
        {
            blockedAppsCache = blockedApps.Distinct<string>("id", new BsonDocument()).ToList();
        }

        public static List<VersionAlias> GetVersionAliases(string appId)
        {
			return versionAliases.Find(x => x.appId == appId).ToList();
		}

		public static List<VersionAlias> GetApplicationsWithAliases()
		{
            List<string> apps = versionAliases.Distinct(x => x.appId, new BsonDocument()).ToList();
            List<VersionAlias> aliases = new List<VersionAlias>();
            for (int i = 0; i < apps.Count; i++)
            {
                BsonDocument d = GetByID(apps[i]).FirstOrDefault();
                if (d == null) continue;
				aliases.Add(new VersionAlias { appId = apps[i], appName = d["display_name"].AsString, appHeadset = (Headset)d["hmd"].AsInt32 });
			}
            return aliases;
		}

		public static VersionAlias GetVersionAlias(string versionId)
		{
			return versionAliases.Find(x => x.versionId == versionId).FirstOrDefault();
		}

		public static void AddVersionAlias(VersionAlias alias)
		{
			versionAliases.DeleteMany(x => x.versionId == alias.versionId);
            versionAliases.InsertOne(alias);
		}

		public static void RemoveVersionAlias(VersionAlias alias)
		{
			versionAliases.DeleteMany(x => x.versionId == alias.versionId);
		}

        public static string AddQAVSReport(QAVSReport report)
        {
            string id = Random.Shared.Next(0x111111, 0xFFFFFF).ToString("X");
            report.reportId = id;
            qAVSReports.DeleteMany(x => x.reportId == id);
            qAVSReports.InsertOne(report);
            return id;
		}

        public static QAVSReport GetQAVSReport(string id)
        {
			return qAVSReports.Find(x => x.reportId == id).FirstOrDefault();
		}

        public static void RemoveIdRemap<T>()
        {
            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                if (typeof(T).GetMember("id").Length > 0)
                {
                    Logger.Log("Unmapping reassignment for " + typeof(T).Name + " id -> _id");
                    cm.UnmapProperty("id");
                    cm.MapMember(typeof(T).GetMember("id")[0])
                        .SetElementName("id")
                        .SetOrder(0) //specific to your needs
                        .SetIsRequired(true); // again specific to your needs
                }
            
                if(typeof(T).GetMember("__id").Length > 0)
                {
                    Logger.Log("Unmapping reassignment for " + typeof(T).Name + " __id -> _id");
                    cm.UnmapProperty("__id");
                    cm.MapMember(typeof(T).GetMember("__id")[0])
                        .SetElementName("__id")
                        .SetOrder(0) //specific to your needs
                        .SetIsRequired(true); // again specific to your needs
                }
            });
        }

        public static void AddAnalytic(Analytic a)
        {
            analyticsCollection.InsertOne(a);
        }

        public static List<Analytic> GetAllAnalyticsForApplication(string parentApplicationId, DateTime after)
        {
            return analyticsCollection.Aggregate<Analytic>(new BsonDocument[]
{
    new BsonDocument("$match",
    new BsonDocument
        {
            { "parentId", parentApplicationId },
            { "reported",
    new BsonDocument("$gte",
    after) }
        }),
    new BsonDocument("$group",
    new BsonDocument
        {
            { "_id",
    new BsonDocument("id", "$itemId") },
            { "itemId",
    new BsonDocument("$first", "$itemId") },
            { "parentId",
    new BsonDocument("$first", "$parentId") },
            { "count",
    new BsonDocument("$sum", 1) }
        })
}).ToList();
        }

        public static List<Analytic> GetApplicationAnalytics(DateTime after, int skip = 0, int take = 50)
        {
            return analyticsCollection.Aggregate<Analytic>(new BsonDocument[]
{
    new BsonDocument("$match",
    new BsonDocument
        {
            { "reported",
    new BsonDocument("$gte",
    after) }
        }),
    new BsonDocument("$group",
    new BsonDocument
        {
            { "_id",
    new BsonDocument("id", "$parentId") },
            { "parentId",
    new BsonDocument("$first", "$parentId") },
            { "applicationName",
    new BsonDocument("$first", "$applicationName") },
            { "count",
    new BsonDocument("$sum", 1) }
        })
}).ToEnumerable().OrderByDescending(x => x.count).Skip(skip).Take(take).ToList();
        }

        public static long CountDataDocuments()
        {
            return versionsCollection.CountDocuments(new BsonDocument()) + applicationCollection.CountDocuments(new BsonDocument()) + dlcCollection.CountDocuments(new BsonDocument())+ dlcPackCollection.CountDocuments(new BsonDocument());
        }

        public static long CountActivityDocuments()
        {
            return activityCollection.CountDocuments(new BsonDocument());
        }

        public static List<DBApplication> GetApplicationByPackageName(string packageName, string currency)
        {
            return applicationCollection.Find(x => x.packageName == packageName).ToList().ConvertAll(x => GetCorrectApplicationEntry(x, currency));
        }

        public static List<DBApplication> GetBestReviews(int skip, int take)
        {
            return applicationCollection.Find(x => true).SortByDescending(x => x.quality_rating_aggregate).Skip(skip).Limit(take).ToList();
        }

        public static List<DBApplication> GetName(int skip, int take)
        {
            return applicationCollection.Find(x => true).SortByDescending(x => x.display_name).Skip(skip).Limit(take).ToList();
        }

        public static List<DBApplication> GetPub(int skip, int take)
        {
            return applicationCollection.Find(x => true).SortByDescending(x => x.publisher_name).Skip(skip).Limit(take).ToList();
        }

        public static List<DBApplication> GetRelease(int skip, int take)
        {
            return applicationCollection.Find(x => true).SortByDescending(x => x.release_date).Skip(skip).Limit(take).ToList();
        }

        public static List<BsonDocument> MongoDBFilterMiddleware(List<BsonDocument> toFilter)
        {
            List<BsonDocument> toReturn = new List<BsonDocument>();
            foreach (BsonDocument b in toFilter)
            {
                string appId = "";
                if (b.Contains("parentApplication"))
                {
                    appId = b.GetValue("parentApplication").AsBsonDocument.GetValue("id").AsString;
                } else if (b.Contains("id"))
                {
                    appId = b.GetValue("id").AsString;
                } else if (b.Contains("oldApplication"))
                {
                    appId = b.GetValue("oldApplication").AsBsonDocument.GetValue("id").AsString;
                }
                if (!blockedAppsCache.Contains(appId))
                {
                    toReturn.Add(b);
                }
            }
            return toReturn;
        }
        
        public static BsonDocument MongoDBFilterMiddleware(BsonDocument toFilter)
        {
            if (toFilter == null) return null;
            string appId = toFilter.Contains("parentApplication") ? toFilter.GetValue("parentApplication").AsBsonDocument.GetValue("id").AsString : toFilter.GetValue("id").AsString;
            if (!blockedAppsCache.Contains(appId))
            {
                return toFilter;
            }
            return null;
        }

        public static List<BsonDocument> GetLatestActivities(int count, int skip = 0, string typeConstraint = "", string applicationId = "", string currency = "")
        {
            string[] stuff = typeConstraint.Split(',');
            BsonArray a = new BsonArray();
			foreach (string s in stuff) a.Add(new BsonDocument("__OculusDBType", s));
            BsonDocument q = new BsonDocument();
			BsonArray and = new BsonArray();
			if (typeConstraint != "") and.Add(new BsonDocument("$or", a));


			if (applicationId != "")
            {
				BsonArray orContitionsForApplication = new BsonArray();
				orContitionsForApplication.Add(new BsonDocument("id", applicationId));
				orContitionsForApplication.Add(new BsonDocument("parentApplication.id", applicationId));
				and.Add(new BsonDocument("$or", orContitionsForApplication));
			}
            if (currency != "")
            {
                BsonDocument currencyFilter = new BsonDocument();
                // Check if currency either doesn't exist or is the specified currency
                BsonArray orContitionsForCurrency = new BsonArray
                {
                    new BsonDocument("currency", new BsonDocument("$exists", false)),
                    new BsonDocument("currency", currency)
                };
                currencyFilter.Add("$or", orContitionsForCurrency);
                and.Add(currencyFilter);
            }
            q.Add(new BsonDocument("$and", and));
            Logger.Log(q.ToJson(), LoggingType.Important);

			return MongoDBFilterMiddleware(activityCollection.Find(q).SortByDescending(x => x["__lastUpdated"]).Skip(skip).Limit(count).ToList());
        }
        public static List<BsonDocument> GetActivityById(string id)
        {
            return MongoDBFilterMiddleware(activityCollection.Find(x => x["_id"] == new ObjectId(id)).ToList());
        }

        public static BsonDocument GetLastEventWithIDInDatabase(string id)
        {
            return MongoDBFilterMiddleware(activityCollection.Find(x => x["id"] == id).SortByDescending(x => x["__lastUpdated"]).FirstOrDefault());
        }
        
        public static BsonDocument GetLastEventWithIDInDatabase(string id, string currency)
        {
            //Logger.Log("Checking currency " + currency + " for " + id, LoggingType.Important);
            return MongoDBFilterMiddleware(activityCollection.Find(x => x["id"] == id && x["currency"] == currency).SortByDescending(x => x["__lastUpdated"]).FirstOrDefault());
        }

		public static BsonDocument GetLastEventWithIDInDatabaseVersion(string id)
		{
			return MongoDBFilterMiddleware(activityCollection.Find(x => x["id"] == id && (x["__OculusDBType"] == DBDataTypes.ActivityVersionUpdated || x["__OculusDBType"] == DBDataTypes.ActivityNewVersion)).SortByDescending(x => x["__lastUpdated"]).FirstOrDefault());
		}

		public static List<BsonDocument> GetLatestActivities(DateTime after)
        {
            return MongoDBFilterMiddleware(activityCollection.Find(x => x["__lastUpdated"] >= after).SortByDescending(x => x["__lastUpdated"]).ToList());
        }

        public static List<ActivityWebhook> GetWebhooks()
        {
            return webhookCollection.Find(new BsonDocument()).ToList();
        }

        public static BsonDocument GetLastPriceChangeOfApp(string appId, string currency)
        {
            return MongoDBFilterMiddleware(activityCollection.Find(x => x["parentApplication"]["id"] == appId && 
                                                                        x["__OculusDBType"] == DBDataTypes.ActivityPriceChanged && 
                                                                        x["currency"] == currency).SortByDescending(x => x["__lastUpdated"]).FirstOrDefault());
        }

        public static List<BsonDocument> GetPriceChanges(string id, string currency)
        {
            return MongoDBFilterMiddleware(activityCollection.Find(x => (x["id"] == id || x["parentApplication"]["id"] == id && x["__OculusDBType"] == DBDataTypes.ActivityPriceChanged) && x["currency"] == currency).SortByDescending(x => x["__lastUpdated"]).ToList());
        }
        
        public static List<BsonDocument> GetByID(string id, int history = 1, string currency = "")
        {
            List<DBVersion> versions = versionsCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList();
            if (versions.Count > 0)
            {
                for(int i = 0; i < versions.Count; i++)
                {
                    VersionAlias a = GetVersionAlias(versions[i].id);
                    versions[i].alias = a == null ? "" : a.alias;
                }
                return MongoDBFilterMiddleware(ToBsonDocumentList(versions));
            }
            List<DBApplication> apps = applicationCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList().ConvertAll(x => GetCorrectApplicationEntry(x, currency));
            if (apps.Count > 0) return ToBsonDocumentList(apps);
            List<DBIAPItem> dlcs = dlcCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList().ConvertAll(x => GetCorrectDLCEntry(x, currency));
            if (dlcs.Count > 0) return MongoDBFilterMiddleware(ToBsonDocumentList(dlcs));
            List<DBIAPItemPack> dlcPacks = dlcPackCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList().ConvertAll(x => GetCorrectDLCPackEntry(x, currency));
            if (dlcPacks.Count > 0) return MongoDBFilterMiddleware(ToBsonDocumentList(dlcPacks));
            return new();
        }

        public static List<BsonDocument> ToBsonDocumentList<T>(List<T> list)
        {
            return list.ConvertAll(x => x.ToBsonDocument());
        }

        public static ConnectedList GetConnected(string id, string currency = "")
        {
            ConnectedList l = new ConnectedList();
            List<BsonDocument> docs = GetByID(id);
            string applicationId = id;
            if(docs.Count() > 0)
			{
				BsonDocument org = docs.First();
                applicationId = org["__OculusDBType"] != DBDataTypes.Application ? org["parentApplication"]["id"].AsString : id;
			}
            if (IsApplicationBlocked(applicationId)) return new ConnectedList();
            l.versions = versionsCollection.Find(x => x.parentApplication.id == applicationId).SortByDescending(x => x.versionCode).ToList();
            l.applications = applicationCollection.Find(x => x.id == applicationId).SortByDescending(x => x.__lastUpdated).ToList();
            l.dlcs = dlcCollection.Find(x => x.parentApplication.id == applicationId).SortByDescending(x => x.__lastUpdated).ToList();
            l.dlcPacks = dlcPackCollection.Find(x => x.parentApplication.id == applicationId).SortByDescending(x => x.__lastUpdated).ToList();

            l.applications.ConvertAll(x => GetCorrectApplicationEntry(x, currency));
            l.dlcs.ConvertAll(x => GetCorrectDLCEntry(x, currency));
            l.dlcPacks.ConvertAll(x => GetCorrectDLCPackEntry(x, currency));
            
            List<VersionAlias> aliases = GetVersionAliases(applicationId);
            for(int i = 0; i < l.versions.Count; i++)
            {
				VersionAlias a = aliases.Find(x => x.versionId == l.versions[i].id);
                if (a != null) l.versions[i].alias = a.alias;
                else l.versions[i].alias = null;
			}

            return l;
        }

        /// <summary>
        /// Returns the DLC pack in the correct currency if it exists, otherwise returns the original DLC pack
        /// </summary>
        private static DBIAPItemPack GetCorrectDLCPackEntry(DBIAPItemPack dbDlcPack, string currency)
        {
            if(currency == "") return dbDlcPack;
            return ScrapingNodeMongoDBManager.GetLocaleDLCPacksCollection(currency).Find(x => x.id == dbDlcPack.id).FirstOrDefault() ?? dbDlcPack;
        }

        /// <summary>
        /// Return the DLC in the correct currency if it exists, otherwise returns the original DLC
        /// </summary>
        private static DBIAPItem GetCorrectDLCEntry(DBIAPItem dbDlc, string currency)
        {
            if(currency == "") return dbDlc;
            return ScrapingNodeMongoDBManager.GetLocaleDLCsCollection(currency).Find(x => x.id == dbDlc.id).FirstOrDefault() ?? dbDlc;
        }
        
        /// <summary>
        /// Return the application in the correct currency if it exists, otherwise returns the original application
        /// </summary>
        private static DBApplication GetCorrectApplicationEntry(DBApplication dbApplication, string currency)
        {
            if(currency == "") return dbApplication;
            return ScrapingNodeMongoDBManager.GetLocaleAppsCollection(currency).Find(x => x.id == dbApplication.id).FirstOrDefault() ?? dbApplication;
        }
        
        

        private static bool IsApplicationBlocked(string id)
        {
            return blockedAppsCache.Contains(id);
        }

        public static DLCLists GetDLCs(string parentAppId, string currency)
        {
            if (IsApplicationBlocked(parentAppId)) return new DLCLists();
            DLCLists l = new();
            l.dlcs = dlcCollection.Find(x => x.parentApplication.id == parentAppId).SortByDescending(x => x.__lastUpdated).ToList();
            l.dlcPacks = dlcPackCollection.Find(x => x.parentApplication.id == parentAppId).SortByDescending(x => x.__lastUpdated).ToList();
            
            l.dlcs.ConvertAll(x => GetCorrectDLCEntry(x, currency));
            l.dlcPacks.ConvertAll(x => GetCorrectDLCPackEntry(x, currency));
            
            return l;
        }

        public static List<DBApplication> GetAllApplications(string currency)
        {
            return applicationCollection.Find(x => true).SortByDescending(x => x.__lastUpdated).ToList().ConvertAll(x => GetCorrectApplicationEntry(x, currency));
        }

        public static List<DBApplication> SearchApplication(string query, List<Headset> headsets, bool quick)
        {
            if (query == "") return new List<DBApplication>();
            if (headsets.Count <= 0) return new List<DBApplication>();
            BsonArray a = new BsonArray();
            foreach (Headset h in headsets) a.Add(new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("hmd", h),
                new BsonDocument("supported_hmd_platforms_enum", h)
            }));
            Regex r = new Regex(".*" + query.Replace(" ", ".*") + ".*", RegexOptions.IgnoreCase);
            return applicationCollection.Find(x => (r.IsMatch(x.display_name) ||r.IsMatch(x.canonicalName) ||r.IsMatch(x.id) || r.IsMatch(x.publisher_name) || r.IsMatch(x.packageName))).ToList().Where(x => x.supported_hmd_platforms_enum.Any(x => headsets.Contains(x))).ToList();
        }

		internal static void CleanDB()
		{
            /*
            //Remove all duplicate apps
            List<string> ids = applicationCollection.Distinct(x => x.id, x => true).ToList();
            Logger.Log("Cleaning " + ids.Count + " applications");
            int i = 0;
            foreach(string id in ids)
            {
                Logger.Log("Cleaning " + id + "(" + i + "/" + ids.Count + " applications)");
                DBApplication newest = applicationCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).First();
                applicationCollection.DeleteMany(x => x.id == id);
                applicationCollection.InsertOne(newest);
				i++;
			}
			*/
            //Remove all duplicate dlcs
            List<string> ids = dlcCollection.Distinct(x => x.id, x => true).ToList();
            Logger.Log("Cleaning " + ids.Count + " dlcs");
            int i = 0;
            foreach(string id in ids)
            {
                Logger.Log("Cleaning " + id + "(" + i + "/" + ids.Count + " dlcs)");
                DBIAPItem newest = dlcCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).First();
                dlcCollection.DeleteMany(x => x.id == id);
                dlcCollection.InsertOne(newest);
                i++;
            }
            
            //Remove all duplicate dlcPacks
            ids = dlcPackCollection.Distinct(x => x.id, x => true).ToList();
            Logger.Log("Cleaning " + ids.Count + " dlcPacks");
            i = 0;
            foreach(string id in ids)
            {
                Logger.Log("Cleaning " + id + "(" + i + "/" + ids.Count + " dlcPacks)");
                DBIAPItemPack newest = dlcPackCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).First();
                dlcPackCollection.DeleteMany(x => x.id == id);
                dlcPackCollection.InsertOne(newest);
                i++;
            }
		}

        public static long GetAppCount()
        {
            return applicationCollection.CountDocuments(x => true);
        }

        public static ScrapeStatus GetScrapeStatus()
        {
            return new ScrapeStatus
            {
                appsToScrape = appsToScrape.Find(x => true).ToList()
            };
        }

        public static List<DBVersion> GetVersions(string appId, bool onlyDownloadableVersions)
        {
            if (IsApplicationBlocked(appId)) return new List<DBVersion>();
            if (onlyDownloadableVersions) return versionsCollection.Find(x => x.parentApplication.id == appId && x.binary_release_channels.nodes.Count > 0).SortByDescending(x => x.versionCode).ToList();
            return versionsCollection.Find(x => x.parentApplication.id == appId).SortByDescending(x => x.versionCode).ToList();
        }

        public static List<DBApplication> GetBlockedApps()
        {
            UpdateBlockedAppsCache();
            return blockedApps.Find(x => true).ToList();
        }

        public static void BlockApp(string id)
        {
            DBApplication app = applicationCollection.Find(x => x.id == id).First();
            blockedApps.InsertOne(app);
            UpdateBlockedAppsCache();
        }
        
        public static void UnblockApp(string id)
        {
            blockedApps.DeleteOne(x => x.id == id);
            UpdateBlockedAppsCache();
        }

        public static bool GetBlockedStatusForApp(string id)
        {
            return blockedAppsCache.Contains(id);
        }

        public static DBAppImage GetAppImage(string appId)
        {
            return appImages.Find(x => x.appId == appId).FirstOrDefault();
        }
    }

    public class ScrapeStatus
    {
        public List<AppToScrape> appsScraping { get; set; } = new();
        public List<AppToScrape> appsToScrape { get; set; } = new();
    }
}
