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
using OculusDB.Users;

namespace OculusDB.MongoDB;

public class OculusDBDatabase
{
    public static MongoClient? mongoClient;
        public static IMongoDatabase? oculusDBDatabase;
        public static IMongoCollection<ActivityWebhook>? webhookCollection;
        public static IMongoCollection<Analytic>? analyticsCollection;
        public static IMongoCollection<AppToScrape>? appsToScrape;
		public static IMongoCollection<VersionAlias>? versionAliases;
        
        public static IMongoCollection<DBApplication>? blockedApps;
        
        public static IMongoCollection<DBApplication>? applicationCollection;
        public static IMongoCollection<DBAppImage>? appImages;
        public static IMongoCollection<DBIAPItem>? iapItemCollection;
        public static IMongoCollection<DBIAPItemPack>? iapItemPackCollection;
        public static IMongoCollection<DBVersion>? versionsCollection;
        public static IMongoCollection<DBAchievement>? achievementCollection;
        public static IMongoCollection<DBOffer>? offerCollection;
        
        public static IMongoCollection<DBDifference>? differenceCollection;

		public static IMongoCollection<QAVSReport>? qAVSReports;
        
        
        public static List<string> blockedAppsCache = new List<string>();
        
        public static void Initialize()
        {
            BsonChunkPool.Default = new BsonChunkPool(512, 1024 * 64);
            ConventionPack pack = new ConventionPack();
            pack.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("Ignore extra elements cause it's annoying", pack, t => true);

            // Don't ask. It's important stuff to reduce DB size and fix a lot of errors
            
            mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
            oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
            webhookCollection = oculusDBDatabase.GetCollection<ActivityWebhook>("webhooks");
            analyticsCollection = oculusDBDatabase.GetCollection<Analytic>("analytics");
            
            versionsCollection = oculusDBDatabase.GetCollection<DBVersion>("versions");
            applicationCollection = oculusDBDatabase.GetCollection<DBApplication>("apps");
            iapItemCollection = oculusDBDatabase.GetCollection<DBIAPItem>("iapItems");
            iapItemPackCollection = oculusDBDatabase.GetCollection<DBIAPItemPack>("iapItemPacks");
            appImages = oculusDBDatabase.GetCollection<DBAppImage>("appImages");
            achievementCollection = oculusDBDatabase.GetCollection<DBAchievement>("achievements");
            offerCollection = oculusDBDatabase.GetCollection<DBOffer>("offers");
            
            differenceCollection = oculusDBDatabase.GetCollection<DBDifference>("differences");

            appsToScrape = oculusDBDatabase.GetCollection<AppToScrape>("appsToScrape");
            qAVSReports = oculusDBDatabase.GetCollection<QAVSReport>("QAVSReports");
            versionAliases = oculusDBDatabase.GetCollection<VersionAlias>("versionAliases");
            blockedApps = oculusDBDatabase.GetCollection<DBApplication>("blockedApps");
            
            RemoveIdRemap<DBAchievement>();
            RemoveIdRemap<DBAchievementTranslation>();
            RemoveIdRemap<DBAppImage>();
            RemoveIdRemap<DBApplication>();
            RemoveIdRemap<DBApplicationGrouping>();
            RemoveIdRemap<DBApplicationTranslation>();
            RemoveIdRemap<DBAssetFile>();
            RemoveIdRemap<DBBase>();
            RemoveIdRemap<DBError>();
            RemoveIdRemap<DBIAPItem>();
            RemoveIdRemap<DBIAPItemId>();
            RemoveIdRemap<DBIAPItemPack>();
            RemoveIdRemap<DBOBBBinary>();
            RemoveIdRemap<DBOffer>();
            RemoveIdRemap<DBParentApplication>();
            RemoveIdRemap<DBParentApplicationGrouping>();
            RemoveIdRemap<DBPrice>();
            RemoveIdRemap<DBReleaseChannel>();
            RemoveIdRemap<DBVersion>();
            RemoveIdRemap<VersionAlias>();
            RemoveIdRemap<QAVSReport>();

            UpdateBlockedAppsCache();
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

        public DBInfo GetDbInfo()
        {
            return new DBInfo
            {
                scrapingStatusPageUrl = OculusDBEnvironment.config.scrapingMasterUrl,
                counts = new Dictionary<string, long>
                {
                    { DBDataTypes.Application, applicationCollection.CountDocuments(x => true) },
                    { DBDataTypes.Version, versionsCollection.CountDocuments(x => true) },
                    { DBDataTypes.IAPItem, iapItemCollection.CountDocuments(x => true) },
                    { DBDataTypes.IAPItemPack, iapItemPackCollection.CountDocuments(x => true) },
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
            
            if (document != null) return document;
            document = versionsCollection.Find(x => x.id == id).FirstOrDefault();
            if (document != null) return document;
            document = iapItemCollection.Find(x => x.id == id).FirstOrDefault();
            if (document != null) return document;
            document = iapItemPackCollection.Find(x => x.id == id).FirstOrDefault();
            if (document != null) return document;
            document = achievementCollection.Find(x => x.id == id).FirstOrDefault();
            if (document != null) return document;
            document = offerCollection.Find(x => x.id == id).FirstOrDefault();
            if (document != null) return document;
            return null;
        }
}