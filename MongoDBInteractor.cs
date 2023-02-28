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
        public static IMongoCollection<AppToScrape>? appsScraping;
        public static IMongoCollection<AppToScrape>? scrapedApps;
		public static IMongoCollection<VersionAlias>? versionAliases;
        
        public static IMongoCollection<DBApplication>? applicationCollection;
        public static IMongoCollection<DBIAPItem>? dlcCollection;
        public static IMongoCollection<DBIAPItemPack>? dlcPackCollection;
        public static IMongoCollection<DBVersion>? versionsCollection;

		public static IMongoCollection<QAVSReport>? qAVSReports;

        public static void MigrateFromDataCollectionToOtherCollections()
        {
            long count = dataCollection.CountDocuments(x => true);
            int i = 0;
            foreach (BsonDocument d in dataCollection.Find(x => true).ToEnumerable())
            {
                Logger.Log("Migrating " + i + " / " + count);
                i++;
                switch (d["__OculusDBType"].AsString)
                {
                    case DBDataTypes.Version:
                        DBVersion v = ObjectConverter.ConvertToDBType(d);
                        versionsCollection.DeleteMany(x => x.id == v.id);
                        versionsCollection.InsertOne(v);
                        continue;
                    case DBDataTypes.Application:
                        DBApplication a = ObjectConverter.ConvertToDBType(d);
                        versionsCollection.DeleteMany(x => x.id == a.id);
                        applicationCollection.InsertOne(a);
                        continue;
                    case DBDataTypes.IAPItem:
                        DBIAPItem dlc = ObjectConverter.ConvertToDBType(d);
                        versionsCollection.DeleteMany(x => x.id == dlc.id);
                        dlcCollection.InsertOne(dlc);
                        continue;
                    case DBDataTypes.IAPItemPack:
                        DBIAPItemPack dlcPack = ObjectConverter.ConvertToDBType(d);
                        versionsCollection.DeleteMany(x => x.id == dlcPack.id);
                        dlcPackCollection.InsertOne(dlcPack);
                        continue;
                    
                }
            }
        }
        
        public static void Initialize()
        {
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

            appsScraping = oculusDBDatabase.GetCollection<AppToScrape>("appsScraping");
            appsToScrape = oculusDBDatabase.GetCollection<AppToScrape>("appsToScrape");
            scrapedApps = oculusDBDatabase.GetCollection<AppToScrape>("scrapedApps");
            qAVSReports = oculusDBDatabase.GetCollection<QAVSReport>("QAVSReports");
            versionAliases = oculusDBDatabase.GetCollection<VersionAlias>("versionAliases");
        }
        
		public static void AddAppToScrapeIfNotPresent(AppToScrape appToScrape)
        {
            if(appToScrape.priority)
            {
                if (appsToScrape.Count(x => x.appId == appToScrape.appId && !x.priority) > 0)
                {
                    appsToScrape.DeleteMany(x => x.appId == appToScrape.appId && !x.priority);
                }

                if (appsToScrape.Count(x => x.appId == appToScrape.appId && x.priority) <= 0 && appsScraping.Count(x => x.appId == appToScrape.appId) <= 0)
                {
                    appsToScrape.InsertOne(appToScrape);
                }
            } else
            {
                if (!IsAppScrapingOrQueuedToScrape(appToScrape))
                {
                    appsToScrape.InsertOne(appToScrape);
                }
            }
        }

		public static void ClearScrapingApps()
		{
			appsScraping.DeleteMany(x => true);
		}

		public static void RemoveScrapingAndToScrapeNonPriorityApps()
        {
            appsToScrape.DeleteMany(x => !x.priority);
            appsScraping.DeleteMany(x => true);
            scrapedApps.DeleteMany(x => true);
        }

        public static AppToScrape GetNextScrapeApp(bool priority)
        {
            return appsToScrape.Find(x => x.priority == priority).SortBy(x => x.addedTime).FirstOrDefault();
        }

        public static void MarkAppAsScraping(AppToScrape app)
        {
            //app._id = ObjectId.GenerateNewId();
            appsToScrape.DeleteMany(x => x.appId == app.appId);
            appsScraping.DeleteMany(x => x.appId == app.appId);
            appsScraping.InsertOne(app);
        }

        public static bool AreAppsToScrapePresent(bool priority)
        {
            return GetAppsToScrapeCount(priority) > 0;
        }

        public static long GetAppsToScrapeCount(bool priority)
        {
            return appsToScrape.Count(x => x.priority == priority);
        }
        public static long GetScrapedAppsCount(bool priority)
        {
            return scrapedApps.Count(x => x.priority == priority);
        }

        public static void MarkAppAsScrapedOrFailed(AppToScrape app)
        {
            appsScraping.DeleteMany(x => x.appId == app.appId);
            if(!app.priority) scrapedApps.InsertOne(app);
        }

        public static bool IsAppScrapingOrQueuedToScrape(AppToScrape app)
        {
            return appsToScrape.Count(x => x.appId == app.appId) + appsScraping.Count(x => x.appId == app.appId) > 0;
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
            string id = Random.Shared.Next(0, 0xFFFFFF).ToString("X");
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
            return versionsCollection.CountDocuments(new BsonDocument())+ applicationCollection.CountDocuments(new BsonDocument()) + dlcCollection.CountDocuments(new BsonDocument())+ dlcPackCollection.CountDocuments(new BsonDocument());
        }

        public static long CountActivityDocuments()
        {
            return activityCollection.CountDocuments(new BsonDocument());
        }

        public static List<DBApplication> GetApplicationByPackageName(string packageName)
        {
            return applicationCollection.Find(x => x.packageName == packageName).ToList();
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

        public static List<BsonDocument> GetLatestActivities(int count, int skip = 0, string typeConstraint = "", string applicationId = "")
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
            q.Add(new BsonDocument("$and", and));

			return activityCollection.Find(q).SortByDescending(x => x["__lastUpdated"]).Skip(skip).Limit(count).ToList();
        }
        public static List<BsonDocument> GetActivityById(string id)
        {
            return activityCollection.Find(x => x["_id"] == new ObjectId(id)).ToList();
        }

        public static BsonDocument GetLastEventWithIDInDatabase(string id)
        {
            return activityCollection.Find(x => x["id"] == id).SortByDescending(x => x["__lastUpdated"]).FirstOrDefault();
        }

		public static BsonDocument GetLastEventWithIDInDatabaseVersion(string id)
		{
			return activityCollection.Find(x => x["id"] == id && (x["__OculusDBType"] == DBDataTypes.ActivityVersionUpdated || x["__OculusDBType"] == DBDataTypes.ActivityNewVersion)).SortByDescending(x => x["__lastUpdated"]).FirstOrDefault();
		}

		public static List<BsonDocument> GetLatestActivities(DateTime after)
        {
            return activityCollection.Find(x => x["__lastUpdated"] >= after).SortByDescending(x => x["__lastUpdated"]).ToList();
        }
        
        public static long DeleteOldApplicationsAndVersionsOfIds(DateTime before, List<string> ids)
        {
            long deleted = 0;
            for(int i = 0; i < ids.Count; i++)
            {
                try
                {
                    // Delete applications, dlcs and dlc packs
                    deleted += applicationCollection.DeleteMany(x => x.__lastUpdated < before && x.id == ids[i]).DeletedCount;
                    deleted += dlcCollection.DeleteMany(x => x.__lastUpdated < before && x.parentApplication.id == ids[i]).DeletedCount;
                    deleted += dlcPackCollection.DeleteMany(x => x.__lastUpdated < before && x.parentApplication.id == ids[i]).DeletedCount;
                }
                catch
                {
                    i--;
                    Logger.Log("Sleeping for 5000 ms before continuing to delete old data due to error");
                    Thread.Sleep(5000);
                }
            }
            return deleted;
        }
        
        public static long DeleteOldVersions(DateTime before, string appId, List<string> versions)
        {
            long deleted = 0;
            
            try
            {
                while(versions.Count > 0)
                {
                    deleted += versionsCollection.DeleteMany(x => x.__lastUpdated < before && x.id == versions[0]).DeletedCount;
                    versions.RemoveAt(0);
                }
            }
            catch
            {
                Logger.Log("Sleeping for 5000 ms before continuing to delete old data due to error");
                Thread.Sleep(5000);
            }
            return deleted;
        }

        public static List<ActivityWebhook> GetWebhooks()
        {
            return webhookCollection.Find(new BsonDocument()).ToList();
        }

        public static BsonDocument GetLastPriceChangeOfApp(string appId)
        {
            return activityCollection.Find(x => x["parentApplication"]["id"] == appId && x["__OculusDBType"] == DBDataTypes.ActivityPriceChanged).SortByDescending(x => x["__lastUpdated"]).FirstOrDefault();
        }

        public static List<BsonDocument> GetPriceChanges(string id)
        {
            return activityCollection.Find(x => (x["id"] == id || x["parentApplication"]["id"] == id && x["__OculusDBType"] == DBDataTypes.ActivityPriceChanged)).SortByDescending(x => x["__lastUpdated"]).ToList();
        }

        public static BsonDocument AddBsonDocumentToActivityCollection(BsonDocument d)
        {
            d["_id"] = ObjectId.GenerateNewId();
            activityCollection.InsertOne(d);
			return activityCollection.Find<BsonDocument>(x => x["_id"] == d["_id"]).FirstOrDefault();
		}

        public static void AddApplication(Application a, Headset h, string image, string packageName)
        {
            DBApplication dba = ObjectConverter.ConvertCopy<DBApplication, Application>(a);
            dba.hmd = h;
            dba.img = image;
            dba.packageName = packageName;
            OculusScraper.DownloadImage(dba);
            applicationCollection.DeleteMany(x => x.id == dba.id);// Delete old entries of the app
            applicationCollection.InsertOne(dba);
        }

        public static void AddVersion(AndroidBinary a, Application app, Headset h, DBVersion oldEntry = null)
        {
            DBVersion dbv = ObjectConverter.ConvertCopy<DBVersion, AndroidBinary>(a);
            dbv.parentApplication.id = app.id;
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
            }

            versionsCollection.InsertOne(dbv);
        }

        public static void AddDLCPack(AppItemBundle a, Headset h, Application app)
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
            dlcPackCollection.InsertOne(dbdlcp);
        }

        public static void AddDLC(IAPItem a, Headset h)
        {
            DBIAPItem dbdlc = ObjectConverter.ConvertCopy<DBIAPItem, IAPItem>(a);
            dbdlc.parentApplication.hmd = h;
            dbdlc.latestAssetFileId = a.latest_supported_asset_file != null ? a.latest_supported_asset_file.id : "";
            dlcCollection.InsertOne(dbdlc);
        }

        public static List<BsonDocument> GetByID(string id, int history = 1)
        {
            List<DBApplication> apps = applicationCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList();
            if (apps.Count > 0) return ToBsonDocumentList(apps);
            List<DBVersion> versions = versionsCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList();
            if (versions.Count > 0)
            {
                for(int i = 0; i < versions.Count; i++)
                {
                    VersionAlias a = GetVersionAlias(versions[i].id);
                    versions[i].alias = a == null ? "" : a.alias;
                }
                return ToBsonDocumentList(versions);
            }
            List<DBIAPItem> dlcs = dlcCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList();
            if (apps.Count > 0) return ToBsonDocumentList(dlcs);
            List<DBIAPItemPack> dlcPacks = dlcPackCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).Limit(history).ToList();
            if (apps.Count > 0) return ToBsonDocumentList(dlcPacks);
            return new();
        }

        public static List<BsonDocument> ToBsonDocumentList<T>(List<T> list)
        {
            return list.ConvertAll(x => x.ToBsonDocument());
        }

        public static ConnectedList GetConnected(string id)
        {
            ConnectedList l = new ConnectedList();
            List<BsonDocument> docs = GetByID(id);
            string applicationId = id;
            if(docs.Count() > 0)
			{
				BsonDocument org = docs.First();
                applicationId = org["__OculusDBType"] != DBDataTypes.Application ? org["parentApplication"]["id"].AsString : id;
			}
            l.versions = versionsCollection.Find(x => x.parentApplication.id == applicationId).SortByDescending(x => x.versionCode).ToList();
            l.applications = applicationCollection.Find(x => x.id == applicationId).SortByDescending(x => x.__lastUpdated).ToList();
            l.dlcs = dlcCollection.Find(x => x.parentApplication.id == applicationId).SortByDescending(x => x.__lastUpdated).ToList();
            l.dlcPacks = dlcPackCollection.Find(x => x.parentApplication.id == applicationId).SortByDescending(x => x.__lastUpdated).ToList();

            List<VersionAlias> aliases = GetVersionAliases(applicationId);
            for(int i = 0; i < l.versions.Count; i++)
            {
				VersionAlias a = aliases.Find(x => x.versionId == l.versions[i].id);
                if (a != null) l.versions[i].alias = a.alias;
                else l.versions[i].alias = null;
			}

            return l;
        }

        public static bool DoesAppIdExistInCurrentScrape(string id)
        {
            return applicationCollection.Find(x => x.id == id && x.__lastUpdated >= OculusDBEnvironment.config.ScrapingResumeData.currentScrapeStart).CountDocuments() > 0;
        }
        
        public static DLCLists GetDLCs(string parentAppId)
        {
            DLCLists l = new();
            l.dlcs = dlcCollection.Find(x => x.parentApplication.id == parentAppId).SortByDescending(x => x.__lastUpdated).ToList();
            l.dlcPacks = dlcPackCollection.Find(x => x.parentApplication.id == parentAppId).SortByDescending(x => x.__lastUpdated).ToList();
            return l;
        }

        public static List<BsonDocument> GetDistinct(IEnumerable<BsonDocument> data)
        {
            List<BsonDocument> distinct = new List<BsonDocument>();
            foreach (BsonDocument d in data)
            {
                if (distinct.FirstOrDefault(x => x["id"] == d["id"]) == null) distinct.Add(d);
            }
            return distinct;
        }

        public static List<DBApplication> GetAllApplications()
        {
            return applicationCollection.Find(x => true).SortByDescending(x => x.__lastUpdated).ToList();
        }

        public static List<DBApplication> SearchApplication(string query, List<Headset> headsets, bool quick)
        {
            if (query == "") return new List<DBApplication>();
            if (headsets.Count <= 0) return new List<DBApplication>();
            BsonDocument regex = new BsonDocument("$regex", new BsonRegularExpression("/.*" + query.Replace(" ", ".*") + ".*/i"));
            BsonArray a = new BsonArray();
            BsonDocument q;
            foreach (Headset h in headsets) a.Add(new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("hmd", h),
                new BsonDocument("parentApplication.hmd", h)
            }));
            Regex r = new Regex(".*" + query.Replace(" ", ".*") + ".*", RegexOptions.IgnoreCase);
            return applicationCollection.Find(x => headsets.Contains(x.hmd) &&  (r.IsMatch(x.display_name) ||r.IsMatch(x.canonicalName) ||r.IsMatch(x.id) || r.IsMatch(x.publisher_name) || r.IsMatch(x.packageName))).ToList();
        }

		internal static void CleanDB()
		{
            //Remove all duplicate apps
            List<string> ids = applicationCollection.Distinct(x => x.id, x => true).ToList();
            Logger.Log("Cleaning " + ids.Count + " applications");
            int i = 0;
            foreach(string id in ids)
            {
                Logger.Log("Cleaning " + id + "(" + i + "/" + ids.Count + ")");
                DBApplication newest = applicationCollection.Find(x => x.id == id).SortByDescending(x => x.__lastUpdated).First();
                applicationCollection.DeleteMany(x => x.id == id);
                applicationCollection.InsertOne(newest);
				i++;
			}
		}

        public static long GetAppCount()
        {
            return applicationCollection.CountDocuments(x => true);
        }
    }
}
