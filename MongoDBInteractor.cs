using ComputerUtils.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using OculusDB.Database;
using OculusDB.Users;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusDB
{
    public class MongoDBInteractor
    {
        public static MongoClient mongoClient = null;
        public static IMongoDatabase oculusDBDatabase = null;
        public static IMongoCollection<BsonDocument> dataCollection = null;
        public static IMongoCollection<BsonDocument> activityCollection = null;
        public static IMongoCollection<DiscordActivityWebhook> webhookCollection = null;

        public static void Initialize()
        {
            mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
            oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
            dataCollection = oculusDBDatabase.GetCollection<BsonDocument>("data");
            webhookCollection = oculusDBDatabase.GetCollection<DiscordActivityWebhook>("webhooks");
            activityCollection = oculusDBDatabase.GetCollection<BsonDocument>("activity");

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
            RemoveIdRemap<DBReleaseChannel>();
            RemoveIdRemap<DBApplication>();
            RemoveIdRemap<DBIAPItem>();

            BsonClassMap.RegisterClassMap<ReleaseChannel>(cm =>
            {
                cm.AutoMap();
                cm.UnmapProperty(x => x.latest_supported_binary); // Remove AndroidBinary
            });
            //RemoveIdRemap<IAPItem>();
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

        public static long CountDataDocuments()
        {
            return dataCollection.CountDocuments(new BsonDocument());
        }

        public static long CountActivityDocuments()
        {
            return activityCollection.CountDocuments(new BsonDocument());
        }

        public static List<BsonDocument> GetLatestActivities(int count, int skip = 0, string typeConstraint = "")
        {
            string[] stuff = typeConstraint.Split(',');
            BsonArray a = new BsonArray();
            foreach (string s in stuff) a.Add(new BsonDocument("__OculusDBType", s));
            BsonDocument q = new BsonDocument();
            if (typeConstraint != "") q.Add(new BsonDocument("$or", a));
            
            
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

        public static List<BsonDocument> GetLatestActivities(DateTime after)
        {
            return activityCollection.Find(x => x["__lastUpdated"] >= after).SortByDescending(x => x["__lastUpdated"]).ToList();
        }

        public static long DeleteOldData(DateTime before, List<string> ids)
        {
            long deleted = 0;
            foreach(string id in ids)
            {
                deleted += dataCollection.DeleteMany(x => x["__lastUpdated"] < before && ((x["__OculusDBType"] == DBDataTypes.Application && x["id"] == id) || (x["__OculusDBType"] != DBDataTypes.Application && x["parentApplication"]["id"] == id))).DeletedCount;
            }
            return deleted;
        }

        public static List<DiscordActivityWebhook> GetWebhooks()
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

        public static void AddBsonDocumentToActivityCollection(BsonDocument d)
        {
            d["_id"] = ObjectId.GenerateNewId();
            activityCollection.InsertOne(d);
        }

        public static void AddApplication(Application a, Headset h)
        {
            DBApplication dba = ObjectConverter.ConvertCopy<DBApplication, Application>(a);
            dba.hmd = h;
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static void AddVersion(AndroidBinary a, Application app, Headset h)
        {
            DBVersion dba = ObjectConverter.ConvertCopy<DBVersion, AndroidBinary>(a);
            dba.parentApplication.id = app.id;
            dba.parentApplication.hmd = h;
            dba.parentApplication.displayName = app.displayName;
            dba.parentApplication.canonicalName = app.canonicalName;
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static void AddDLCPack(AppItemBundle a, Headset h, Application app)
        {
            DBIAPItemPack dba = ObjectConverter.ConvertCopy<DBIAPItemPack, AppItemBundle, IAPItem>(a);
            dba.parentApplication.hmd = h;
            dba.parentApplication.displayName = app.displayName;
            foreach(Node<IAPItem> i in a.bundle_items.edges)
            {
                DBItemId id = new DBItemId();
                id.id = i.node.id;
                dba.bundle_items.Add(id);
            }
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static void AddDLC(IAPItem a, Headset h)
        {
            DBIAPItem dba = ObjectConverter.ConvertCopy<DBIAPItem, IAPItem>(a);
            dba.parentApplication.hmd = h;
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static List<BsonDocument> GetByID(string id, int history = 1)
        {
            return dataCollection.Find(new BsonDocument("id", id)).SortByDescending(x => x["__lastUpdated"]).Limit(history).ToList();
        }

        public static ConnectedList GetConnected(string id)
        {
            ConnectedList l = new ConnectedList();
            BsonDocument org = GetByID(id).First();
            BsonDocument q = new BsonDocument
            {
                new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument("id", id),
                    new BsonDocument("id", org["__OculusDBType"] != DBDataTypes.Application ? org["parentApplication"]["id"] : "yourMom"),
                    new BsonDocument("parentApplication.id", org["__OculusDBType"] != DBDataTypes.Application ? org["parentApplication"]["id"] : id)
                })
            };
            foreach(BsonDocument d in GetDistinct(dataCollection.Find(q).SortByDescending(x => x["__lastUpdated"]).ToEnumerable()))
            {
                if(d["__OculusDBType"] == DBDataTypes.Version) l.versions.Add(ObjectConverter.ConvertToDBType(d));
                else if(d["__OculusDBType"] == DBDataTypes.Application) l.applications.Add(ObjectConverter.ConvertToDBType(d));
                else if (d["__OculusDBType"] == DBDataTypes.IAPItemPack) l.dlcPacks.Add(ObjectConverter.ConvertToDBType(d));
                else if (d["__OculusDBType"] == DBDataTypes.IAPItem) l.dlcs.Add(ObjectConverter.ConvertToDBType(d));
            }
            l.versions = l.versions.OrderByDescending(x => x.versionCode).ToList();
            return l;
        }

        public static bool DoesIdExistInCurrentScrape(string id)
        {
            return dataCollection.Find(x => x["id"] == id && x["__lastUpdated"] >= OculusDBEnvironment.config.ScrapingResumeData.currentScrapeStart).CountDocuments() > 0;
        }

        public static DLCLists GetDLCs(string parentAppId)
        {
            BsonDocument q = new BsonDocument
            {
                new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument("__OculusDBType", DBDataTypes.IAPItem),
                    new BsonDocument("__OculusDBType", DBDataTypes.IAPItemPack)
                }),
                new BsonElement("parentApplication.id", parentAppId)
                
            };

            DLCLists dlcs = new DLCLists();
            foreach (BsonDocument doc in GetDistinct(dataCollection.Find(q).SortByDescending(x => x["__lastUpdated"]).ToEnumerable()))
            {
                if (doc["__OculusDBType"] == DBDataTypes.IAPItem) dlcs.dlcs.Add(ObjectConverter.ConvertToDBType(doc));
                else dlcs.dlcPacks.Add(ObjectConverter.ConvertToDBType(doc));
            }
            return dlcs;
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

        public static List<BsonDocument> SearchApplication(string query, List<Headset> headsets)
        {
            if (query == "") return new List<BsonDocument>();
            if (headsets.Count <= 0) return new List<BsonDocument>();
            BsonDocument regex = new BsonDocument("$regex", new BsonRegularExpression("/.*" + query.Replace(" ", ".*") + ".*/i"));
            BsonArray a = new BsonArray();
            foreach (Headset h in headsets) a.Add(new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("hmd", h),
                new BsonDocument("parentApplication.hmd", h)
            }));
            BsonDocument q = new BsonDocument() { new BsonDocument("$and", new BsonArray {
                new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("__OculusDBType", DBDataTypes.Application),
                new BsonDocument("__OculusDBType", DBDataTypes.IAPItem),
                new BsonDocument("__OculusDBType", DBDataTypes.IAPItemPack)
            }), new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("displayName", regex),
                new BsonDocument("canonicalName", regex),
                new BsonDocument("publisher_name", regex),
                new BsonDocument("id", query),

            }),
                new BsonDocument("$or", a)
            })};
            return GetDistinct(dataCollection.Find(q).SortByDescending(x => x["__lastUpdated"]).ToEnumerable());
        }
    }
}
