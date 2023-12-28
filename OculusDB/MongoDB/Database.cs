using ComputerUtils.Logging;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using OculusDB.Analytics;
using OculusDB.Database;
using OculusDB.ObjectConverters;
using OculusDB.QAVS;
using OculusDB.ScrapingMaster;
using OculusDB.Users;

namespace OculusDB.MongoDB;

public class OculusDBDatabase
{
    public static MongoClient? mongoClient;
        public static IMongoDatabase? oculusDBDatabase;
        public static IMongoCollection<DifferenceWebhook>? webhookCollection;
        public static IMongoCollection<Analytic>? analyticsCollection;
        public static IMongoCollection<AppToScrape>? appsToScrape;
		public static IMongoCollection<VersionAlias>? versionAliases;
        
        public static IMongoCollection<DBApplication>? blockedApps;
        
        public static IMongoCollection<DBApplication>? applicationCollection;
        public static IMongoCollection<DBAppImage>? appImages;
        public static IMongoCollection<DBIapItem>? iapItemCollection;
        public static IMongoCollection<DBIapItemPack>? iapItemPackCollection;
        public static IMongoCollection<DBVersion>? versionCollection;
        public static IMongoCollection<DBAchievement>? achievementCollection;
        public static IMongoCollection<DBOffer>? offerCollection;
        
        public static IMongoCollection<DBDifference>? differenceCollection;

		public static IMongoCollection<QAVSReport>? qAVSReports;
        

        public static IMongoCollection<ScrapingNode> scrapingNodes;
        public static IMongoCollection<ScrapingNodeStats> scrapingNodeStats;
        public static IMongoCollection<ScrapingContribution> scrapingNodeContributions;
        public static IMongoCollection<ScrapingProcessingStats> scrapingProcessingStats;
        public static IMongoCollection<AppToScrape> appsScraping;
        public static IMongoCollection<ScrapingNodeOverrideSettings> scrapingNodeOverrideSettingses;
        public static IMongoCollection<ScrapingError> scrapingErrors;
        
        
        public static List<string> blockedAppsCache = new List<string>();
        private static bool initialized = false;
        public static IMongoCollection<ScrapingNodeApplicationNull> applicationNullCollection;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            BsonChunkPool.Default = new BsonChunkPool(512, 1024 * 64);
            ConventionPack pack = new ConventionPack();
            pack.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("Ignore extra elements cause it's annoying", pack, t => true);

            // Don't ask. It's important stuff to reduce DB size and fix a lot of errors
            
            
            RemoveIdRemap<DBAchievement>();
            RemoveIdRemap<DBAchievementTranslation>();
            RemoveIdRemap<DBAppImage>();
            RemoveIdRemap<DBApplication>();
            RemoveIdRemap<DBApplicationGrouping>();
            RemoveIdRemap<DBApplicationTranslation>();
            RemoveIdRemap<DBAssetFile>();
            RemoveIdRemap<DBBase>();
            RemoveIdRemap<DBError>();
            RemoveIdRemap<DBIapItem>();
            RemoveIdRemap<DBIAPItemId>();
            RemoveIdRemap<DBIapItemPack>();
            RemoveIdRemap<DBOBBBinary>();
            RemoveIdRemap<DBOffer>();
            RemoveIdRemap<DBParentApplication>();
            RemoveIdRemap<DBParentApplicationGrouping>();
            RemoveIdRemap<DBPrice>();
            RemoveIdRemap<DBReleaseChannel>();
            RemoveIdRemap<DBVersion>();
            RemoveIdRemap<VersionAlias>();
            RemoveIdRemap<QAVSReport>();
            RemoveIdRemap<ScrapingNodeApplicationNull>();
            
            mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
            oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
            webhookCollection = oculusDBDatabase.GetCollection<DifferenceWebhook>("webhooks");
            analyticsCollection = oculusDBDatabase.GetCollection<Analytic>("analytics");
            
            versionCollection = oculusDBDatabase.GetCollection<DBVersion>("versions");
            applicationCollection = oculusDBDatabase.GetCollection<DBApplication>("apps");
            iapItemCollection = oculusDBDatabase.GetCollection<DBIapItem>("iapItems");
            iapItemPackCollection = oculusDBDatabase.GetCollection<DBIapItemPack>("iapItemPacks");
            appImages = oculusDBDatabase.GetCollection<DBAppImage>("appImages");
            achievementCollection = oculusDBDatabase.GetCollection<DBAchievement>("achievements");
            offerCollection = oculusDBDatabase.GetCollection<DBOffer>("offers");
            
            differenceCollection = oculusDBDatabase.GetCollection<DBDifference>("differences");

            appsToScrape = oculusDBDatabase.GetCollection<AppToScrape>("appsToScrape");
            qAVSReports = oculusDBDatabase.GetCollection<QAVSReport>("QAVSReports");
            versionAliases = oculusDBDatabase.GetCollection<VersionAlias>("versionAliases");
            blockedApps = oculusDBDatabase.GetCollection<DBApplication>("blockedApps");
            
            scrapingNodes = oculusDBDatabase.GetCollection<ScrapingNode>("scrapingNodes");
            scrapingNodeStats = oculusDBDatabase.GetCollection<ScrapingNodeStats>("scrapingStats");
            scrapingNodeContributions = oculusDBDatabase.GetCollection<ScrapingContribution>("scrapingContributions");
            scrapingProcessingStats = oculusDBDatabase.GetCollection<ScrapingProcessingStats>("scrapingProcessingStats");
            appsScraping = oculusDBDatabase.GetCollection<AppToScrape>("appsScraping");
            scrapingNodeOverrideSettingses = oculusDBDatabase.GetCollection<ScrapingNodeOverrideSettings>("scrapingNodeOverrideSettingses");
            scrapingErrors = oculusDBDatabase.GetCollection<ScrapingError>("scrapingErrors");
            applicationNullCollection = oculusDBDatabase.GetCollection<ScrapingNodeApplicationNull>("applicationNulls");
            
            
            

            UpdateBlockedAppsCache();
        }
        
        public static bool IsApplicationBlocked(string id)
        {
            return blockedAppsCache.Contains(id);
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
        public static List<DBApplication> GetBlockedApps()
        {
            UpdateBlockedAppsCache();
            return blockedApps.Find(x => true).ToList();
        }
        public static List<DBApplication> GetAllApplications()
        {
            return applicationCollection.Find(x => true).SortByDescending(x => x.__lastUpdated).ToList();
        }
        
        public static void UpdateBlockedAppsCache()
        {
            blockedAppsCache = blockedApps?.Distinct<string>("id", new BsonDocument()).ToList() ?? new List<string>();
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

        public static DBInfo GetDbInfo()
        {
            return new DBInfo
            {
                scrapingStatusPageUrl = OculusDBEnvironment.config.scrapingMasterUrl,
                counts = new Dictionary<string, long>
                {
                    { DBDataTypes.Application, applicationCollection.CountDocuments(x => true) },
                    { DBDataTypes.Version, versionCollection.CountDocuments(x => true) },
                    { DBDataTypes.IapItem, iapItemCollection.CountDocuments(x => true) },
                    { DBDataTypes.IapItemPack, iapItemPackCollection.CountDocuments(x => true) },
                    { DBDataTypes.Achievement, achievementCollection.CountDocuments(x => true) },
                    { DBDataTypes.Offer, offerCollection.CountDocuments(x => true) },
                    { DBDataTypes.AppImage, appImages.CountDocuments(x => true) },
                    { DBDataTypes.Difference, differenceCollection.CountDocuments(x => true) },
                    { DBDataTypes.VersionAlias, versionAliases.CountDocuments(x => true) },
                }
            };
        }
        
        public static DBBase? GetDocument(string id)
        {
            DBBase? document = applicationCollection.Find(x => x.id == id).FirstOrDefault();
            
            if (document == null)
                document = versionCollection.Find(x => x.id == id).FirstOrDefault();
            if (document == null) 
                document = iapItemCollection.Find(x => x.id == id).FirstOrDefault();
            if (document == null)
                document = iapItemPackCollection.Find(x => x.id == id).FirstOrDefault();
            if (document == null)
                document = achievementCollection.Find(x => x.id == id).FirstOrDefault();
            if (document == null)
                document = offerCollection.Find(x => x.id == id).FirstOrDefault();
            if (document != null)
            {
                document.PopulateSelf(new PopulationContext());
                return document;
            }
            return null;
        }

        public static List<DifferenceWebhook> GetAllWebhooks()
        {
            return webhookCollection.Find(x => true).ToList();
        }

        /// <summary>
        /// Creates or updates an webhook based on __id
        /// </summary>
        /// <param name="webhook">Webhook to update/create</param>
        /// <returns>Message for user</returns>
        public static DifferenceWebhookResponse AddOrCreateWebhook(DifferenceWebhook webhook)
        {
            if (webhook.__id == "")
            {
                
                webhookCollection.InsertOne(webhook);
                return new DifferenceWebhookResponse { msg = "Webhook created", isNewWebhook = true };
            }
            webhookCollection.ReplaceOne(x => x.__id == webhook.__id, webhook);
            return new DifferenceWebhookResponse { msg = "Webhook updated", isNewWebhook = false };
        }

        public static List<DBDifference> GetDiffsFromQueue(int limit)
        {
            List<DBDifference> diffs = differenceCollection.Find(x => !x.webhookProcessed).Limit(limit).ToList();
            return diffs;
        }
        
        public static void SetDiffProcessed(DBDifference diff)
        {
            differenceCollection.UpdateOne(x => x.__id == diff.__id, Builders<DBDifference>.Update.Set(x => x.webhookProcessed, true));
        }

        public static DifferenceWebhookResponse DeleteWebhook(DifferenceWebhook? webhook)
        {
            if (webhook == null) return new DifferenceWebhookResponse { msg = "Webhook not found", isNewWebhook = false };
            webhookCollection.DeleteOne(x => x.__id == webhook.__id);
            return new DifferenceWebhookResponse { msg = "Webhook deleted", isNewWebhook = false };
        }
}