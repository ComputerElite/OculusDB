using ComputerUtils.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using OculusDB.Database;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static IMongoCollection<BsonDocument> userCollection = null;

        public static void Initialize()
        {
            mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
            oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
            dataCollection = oculusDBDatabase.GetCollection<BsonDocument>("data");
            userCollection = oculusDBDatabase.GetCollection<BsonDocument>("users");

            ConventionPack pack = new ConventionPack();
            pack.Add(new IgnoreExtraElementsConvention(true));
            ConventionRegistry.Register("Ignore extra elements cause it's annoying", pack, t => true);

            RemoveIdRemap<IAPItem>();
            RemoveIdRemap<Application>();
            RemoveIdRemap<ParentApplication>();
            RemoveIdRemap<AndroidBinary>();
            RemoveIdRemap<AppStoreOffer>();
        }


        public static void RemoveIdRemap<T>()
        {
            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                cm.UnmapProperty("id");
                cm.MapMember(typeof(T).GetMember("id")[0])
                    .SetElementName("id")
                    .SetOrder(0) //specific to your needs
                    .SetIsRequired(true); // again specific to your needs
            });
        }

        public static void AddApplication(Application a)
        {
            DBApplication dba = ObjectConverter.Convert<DBApplication, Application>(a);
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static void AddVersion(AndroidBinary a, Application app)
        {
            DBVersion dba = ObjectConverter.Convert<DBVersion, AndroidBinary>(a);
            dba.uri = "https://securecdn.oculus.com/binaries/download/?id=" + dba.id;
            dba.parentApplication.id = app.id;
            dba.parentApplication.canonicalName = app.canonicalName;
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static void AddDLCPack(AppItemBundle a)
        {
            DBIAPItemPack dba = ObjectConverter.Convert<DBIAPItemPack, AppItemBundle, IAPItem>(a);
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static void AddDLC(IAPItem a)
        {
            DBIAPItem dba = ObjectConverter.Convert<DBIAPItem, IAPItem>(a);
            dataCollection.InsertOne(dba.ToBsonDocument());
        }

        public static List<BsonDocument> GetByID(string id, int history = 1)
        {
            return dataCollection.Find(new BsonDocument("id", id)).SortByDescending(x => x["__lastUpdated"]).Limit(history).ToList();
        }

        public static ConnectedList GetConnected(string id)
        {
            ConnectedList l = new ConnectedList();
            BsonDocument q = new BsonDocument
            {
                new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument("id", id),
                    new BsonDocument("parentApplication.id", id)
                }),
                GetLastTimeFilter()
            };
            foreach(BsonDocument d in dataCollection.Find(q).ToList())
            {
                if(d["__OculusDBType"] == DBDataTypes.Version) l.versions.Add(ObjectConverter.ConvertToDBType(d));
                else if(d["__OculusDBType"] == DBDataTypes.Application) l.applications.Add(ObjectConverter.ConvertToDBType(d));
                else if (d["__OculusDBType"] == DBDataTypes.IAPItemPack) l.dlcPacks.Add(ObjectConverter.ConvertToDBType(d));
                else if (d["__OculusDBType"] == DBDataTypes.IAPItem) l.dlcs.Add(ObjectConverter.ConvertToDBType(d));
            }
            return l;
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
                new BsonElement("parentApplication.id", parentAppId),
                GetLastTimeFilter()
                
            };

            DLCLists dlcs = new DLCLists();
            foreach (BsonDocument doc in dataCollection.Find(q).SortByDescending(x => x["__lastUpdated"]).ToEnumerable())
            {
                if (doc["__OculusDBType"] == DBDataTypes.IAPItem) dlcs.dlcs.Add(ObjectConverter.ConvertToDBType(doc));
                else dlcs.dlcPacks.Add(ObjectConverter.ConvertToDBType(doc));
            }
            return dlcs;
        }

        public static BsonDocument GetLastTimeFilter()
        {
            return new BsonDocument("__lastUpdated", new BsonDocument("$gte", OculusDBEnvironment.config.lastDBUpdate));
        }

        public static List<BsonDocument> SearchApplication(string query)
        {
            BsonDocument regex = new BsonDocument("$regex", new BsonRegularExpression("/.*" + query + ".*/i"));
            BsonDocument q = new BsonDocument() { new BsonDocument("__OculusDBType", DBDataTypes.Application), new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("displayName", regex),
                new BsonDocument("canonicalName", regex),
                new BsonDocument("publisher_name", regex),
                new BsonDocument("id", query),
                
            }), GetLastTimeFilter() };
            return dataCollection.Find(q).SortByDescending(x => x["__lastUpdated"]).ToList();
        }
    }
}
