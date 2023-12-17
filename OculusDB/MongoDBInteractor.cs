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
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusDB.ScrapingMaster;

namespace OculusDB
{
    public class MongoDBInteractor
    {
        public static void AddAnalytic(Analytic a)
        {
            OculusDBDatabase.analyticsCollection.InsertOne(a);
        }

        public static List<Analytic> GetAllAnalyticsForApplication(string parentApplicationId, DateTime after)
        {
            return OculusDBDatabase.analyticsCollection.Aggregate<Analytic>(new BsonDocument[]
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
            return OculusDBDatabase.analyticsCollection.Aggregate<Analytic>(new BsonDocument[]
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

        /// <summary>
        /// Filters out everything about blocked apps. All public api responses should be passed through this
        /// </summary>
        /// <param name="toFilter">Array to filter</param>
        /// <returns>Filtered List</returns>
        public static List<dynamic> MongoDBFilterMiddleware(List<dynamic> toFilter)
        {
            List<dynamic> toReturn = new List<dynamic>();
            foreach (dynamic d in toFilter)
            {
                string json = JsonSerializer.Serialize(d);
                if(OculusDBDatabase.blockedAppsCache.Any(x => json.Contains(x))) continue;
                toReturn.Add(d);
            }
            return toReturn;
        }
        
        public static dynamic MongoDBFilterMiddleware(dynamic toFilter)
        {
            string json = JsonSerializer.Serialize(toFilter);
            if(OculusDBDatabase.blockedAppsCache.Any(x => json.Contains(x))) return null;
            return toFilter;
        }

        public static List<ActivityWebhook> GetWebhooks()
        {
            return OculusDBDatabase.webhookCollection.Find(new BsonDocument()).ToList();
        }
        
        

        public static DLCLists? GetDlcs(string id)
        {
            DLCLists l = new DLCLists();
            // Gets everything connected with any type
            // If the id just has an application group we will return everything for all applications
            // If the id has an application we will just return everything for that application
            
            // Step 1: Get the object by id.
            DBBase? foundObject = OculusDBDatabase.GetDocument(id);
            if(foundObject == null) return null;
            ApplicationContext applicationContext = foundObject.GetApplicationIds();
            
            // Get population context with stuff needed for multiple entries to populate themselves. Just using one query per type instead of multiple per id
            PopulationContext context = PopulationContext.GetForApplicationContext(applicationContext);
            
            // Step 2: Get all the connected objects
            l.iapItems = DBIAPItem.GetAllForApplicationGrouping(applicationContext.groupingId);
            l.iapItemPacks = DBIAPItemPack.GetAllForApplicationGrouping(applicationContext.groupingId);
            
            l.PopulateAll(context);
            return l;
        }

        public static ConnectedList? GetConnected(string id)
        {
            ConnectedList l = new ConnectedList();
            // Gets everything connected with any type
            // If the id just has an application group we will return everything for all applications
            // If the id has an application we will just return everything for that application
            
            // Step 1: Get the object by id.
            DBBase? foundObject = OculusDBDatabase.GetDocument(id);
            if(foundObject == null) return null;
            ApplicationContext applicationContext = foundObject.GetApplicationIds();
            
            // Get population context with stuff needed for multiple entries to populate themselves. Just using one query per type instead of multiple per id
            PopulationContext context = PopulationContext.GetForApplicationContext(applicationContext);
            
            // Step 2: Get all the connected objects
            l.applications = OculusDBDatabase.applicationCollection.Find(x => applicationContext.appIds.Contains(x.id) || (x.grouping != null && x.grouping.id == applicationContext.groupingId)).ToList();
            l.iapItems = DBIAPItem.GetAllForApplicationGrouping(applicationContext.groupingId);
            l.iapItemPacks = DBIAPItemPack.GetAllForApplicationGrouping(applicationContext.groupingId);
            l.achievements = DBAchievement.GetAllForApplicationGrouping(applicationContext.groupingId);
            l.versions = DBVersion.GetVersionsOfAppIds(applicationContext.appIds);
            
            l.PopulateAll(context);
            return l;
        }

        /// <summary>
        /// Returns the DLC pack in the correct currency if it exists, otherwise returns the original DLC pack
        /// </summary>
        private static DBIAPItemPack GetCorrectDLCPackEntry(DBIAPItemPack dbDlcPack, string currency)
        {
            if(currency == "") return dbDlcPack;
            return null;
        }

        /// <summary>
        /// Return the DLC in the correct currency if it exists, otherwise returns the original DLC
        /// </summary>
        private static DBIAPItem GetCorrectDLCEntry(DBIAPItem dbDlc, string currency)
        {
            if(currency == "") return dbDlc;
            return null;
        }
        
        /// <summary>
        /// Return the application in the correct currency if it exists, otherwise returns the original application
        /// </summary>
        private static DBApplication GetCorrectApplicationEntry(DBApplication dbApplication, string currency)
        {
            if(currency == "") return dbApplication;
            return null;
        }

        public static List<DBApplication> SearchApplication(string query, List<Headset> headsets, List<HeadsetGroup> headsetGroups, bool quick)
        {
            if (query == "") return new List<DBApplication>();
            // If headset groups are given, ignore headsets
            if(headsetGroups.Count > 0) headsets = new List<Headset>();
            if (headsets.Count <= 0 && headsetGroups.Count <= 0) return new List<DBApplication>();
            Regex r = new Regex(".*" + query.Replace(" ", ".*") + ".*", RegexOptions.IgnoreCase);
            return null;
        }

        public static List<DBVersion> GetVersions(string appId, bool onlyDownloadableVersions)
        {
            if (OculusDBDatabase.IsApplicationBlocked(appId)) return new List<DBVersion>();
            if (onlyDownloadableVersions) return OculusDBDatabase.versionCollection.Find(x => x.parentApplication != null && x.parentApplication.id == appId && x.releaseChannels.Count > 0).SortByDescending(x => x.versionCode).ToList();
            return OculusDBDatabase.versionCollection.Find(x => x.parentApplication != null && x.parentApplication.id == appId).SortByDescending(x => x.versionCode).ToList();
        }

        public static List<DBDifference> GetLatestDiffs(int count, int skip, string typeConstraint, string application, string currency)
        {
            throw new NotImplementedException();
        }

        public static Dictionary<string, List<DBOffer>> GetFormerPricesOfId(string id)
        {
            ConnectedList l = new ConnectedList();
            // Gets everything connected with any type
            // If the id just has an application group we will return everything for all applications
            // If the id has an application we will just return everything for that application
            
            // Step 1: Get the object by id.
            DBBase? foundObject = OculusDBDatabase.GetDocument(id);
            if(foundObject == null) return null;
            
            // 1. Get all former offer ids
            Dictionary<string, List<DBOffer>> priceChanges = new Dictionary<string, List<DBOffer>>();
            List<DBDifference> applicationDifferences = OculusDBDatabase.differenceCollection.Find(x => x.entryId == id).ToList();
            applicationDifferences = applicationDifferences.Where(x =>
                x.differenceType == DifferenceType.ObjectAdded || x.entries.Any(x => x.name == "offerId")).ToList();
            // Get distinct offer ids
            List<string> offerIds = applicationDifferences.Select(x => ((DBApplication)x.newObject).offerId).Distinct().ToList();
            // 2. Get all former offers
            List<DBDifference> offerDifferences = OculusDBDatabase.differenceCollection.Find(x => offerIds.Contains(x.entryId)).ToList();
            List<DBOffer?> dbOffers = offerDifferences.Select(x => (DBOffer?)x.newObject).ToList();
            // 3. Get all former prices grouped by currency
            foreach (DBOffer offer in dbOffers.OrderByDescending(x => x.__lastUpdated))
            {
                if(!priceChanges.ContainsKey(offer.currency)) priceChanges[offer.currency] = new List<DBOffer>();
                priceChanges[offer.currency].Add(offer);
            }

            return priceChanges;
        }
    }

    public class ScrapeStatus
    {
        public List<AppToScrape> appsScraping { get; set; } = new();
        public List<AppToScrape> appsToScrape { get; set; } = new();
    }
}
