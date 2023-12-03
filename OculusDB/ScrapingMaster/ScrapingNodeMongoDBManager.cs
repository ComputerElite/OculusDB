using ComputerUtils.Logging;
using ComputerUtils.RandomExtensions;
using MongoDB.Bson;
using MongoDB.Driver;
using OculusDB.Database;
using OculusDB.Users;

namespace OculusDB.ScrapingMaster;

public class ScrapingNodeMongoDBManager
{
    public static IMongoClient mongoClient;
    public static IMongoDatabase oculusDBDatabase;
    public static IMongoCollection<ScrapingNode> scrapingNodes;
    public static IMongoCollection<ScrapingNodeStats> scrapingNodeStats;
    public static IMongoCollection<ScrapingContribution> scrapingNodeContributions;
    public static IMongoCollection<ScrapingProcessingStats> scrapingProcessingStats;
    public static IMongoCollection<AppToScrape> appsScraping;
    public static IMongoCollection<ScrapingNodeOverrideSettings> scrapingNodeOverrideSettingses;
    public static IMongoCollection<ScrapingError> scrapingErrors;

    public static void Init()
    {
        mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
        oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
        scrapingNodes = oculusDBDatabase.GetCollection<ScrapingNode>("scrapingNodes");
        scrapingNodeStats = oculusDBDatabase.GetCollection<ScrapingNodeStats>("scrapingStats");
        scrapingNodeContributions = oculusDBDatabase.GetCollection<ScrapingContribution>("scrapingContributions");
        scrapingProcessingStats = oculusDBDatabase.GetCollection<ScrapingProcessingStats>("scrapingProcessingStats");
        appsScraping = oculusDBDatabase.GetCollection<AppToScrape>("appsScraping");
        scrapingNodeOverrideSettingses = oculusDBDatabase.GetCollection<ScrapingNodeOverrideSettings>("scrapingNodeOverrideSettingses");
        scrapingErrors = oculusDBDatabase.GetCollection<ScrapingError>("scrapingErrors");

        CleanAppsScraping();
        //UpdateAllExistingAppsWithGroupAndBinaryType();
    }

    public static void UpdateAllExistingAppsWithGroupAndBinaryType()
    {
        
    }

    public static void CheckActivityCollection()
    {
        
    }

    public static void AddScrapingProcessingStat(ScrapingProcessingStats scrapingProcessingStat)
    {
        scrapingProcessingStats.InsertOne(scrapingProcessingStat);
    }

    public static List<ScrapingProcessingStats> GetScrapingProcessingStats()
    {
        return scrapingProcessingStats.Find(x => true).Limit(1000).ToList();
    }

    public static long GetNonPriorityAppsToScrapeCount(string currency)
    {
        return MongoDBInteractor.appsToScrape.CountDocuments(x => !x.priority && x.currency == currency);
    }

    public static List<AppToScrape> GetAppsToScrapeAndAddThemToScrapingApps(bool priority, int count, ScrapingNode responsibleForApps)
    {
        // Get apps to scrape
        string currency = responsibleForApps.currency;
        List<AppToScrape> appsToScrape =
            MongoDBInteractor.appsToScrape.Find(x => x.priority == priority && (x.currency == currency || x.currency == "")).SortByDescending(x => x.scrapePriority).Limit(count).ToList();
        // Set responsible scraping node, sent time and remove from apps to scrape
        DateTime now = DateTime.UtcNow;
        List<AppToScrape> selected = new();
        // Add apps to Scraping apps and remove them from apps to scrape
        if (appsToScrape.Count > 0)
        {
            appsToScrape.ForEach(x =>
            {
                x.responsibleScrapingNodeId = responsibleForApps.scrapingNodeId;
                x.sentToScrapeTime = now;
                appsScraping.DeleteMany(y => y.appId == x.appId && y.priority == x.priority && y.currency == x.currency);
                appsScraping.InsertOne(x);
                if(!selected.Any(y => x.appId == y.appId && x.priority == y.priority))
                    selected.Add(x);
                MongoDBInteractor.appsToScrape.DeleteOne(y => y.appId == x.appId && y.priority == x.priority && y.currency == x.currency);
            });
        }
        
        return selected;
    }
    
    public static ScrapingNodeAuthenticationResult CheckScrapingNode(ScrapingNodeIdentification scrapingNodeIdentification)
    {
        DateTime now = DateTime.UtcNow;
        ScrapingNode scrapingNode = scrapingNodes.Find(x => x.scrapingNodeToken == scrapingNodeIdentification.scrapingNodeToken).FirstOrDefault();
        if (scrapingNode == null)
        {
            // Token not found
            return new ScrapingNodeAuthenticationResult
            {
                tokenExpired = false,
                tokenAuthorized = false,
                tokenValid = false,
                msg = "Token not found. Contact ComputerElite if you want to help scraping.",
                scrapingNode = new ScrapingNode
                {
                    scrapingNodeToken = scrapingNodeIdentification.scrapingNodeToken,
                    scrapingNodeVersion = scrapingNodeIdentification.scrapingNodeVersion,
                    currency = scrapingNodeIdentification.currency
                },
                compatibleScrapingVersion = OculusDBEnvironment.updater.version
            };
        }
        ScrapingNodeOverrideSettings scrapingNodeOverrideSettings = scrapingNodeOverrideSettingses.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if(scrapingNodeOverrideSettings == null) scrapingNodeOverrideSettings = new ScrapingNodeOverrideSettings();
        
        scrapingNode.currency = scrapingNodeIdentification.currency;
        scrapingNode.scrapingNodeVersion = scrapingNodeIdentification.scrapingNodeVersion;
        scrapingNodes.ReplaceOne(x => x.scrapingNodeToken == scrapingNodeIdentification.scrapingNodeToken, scrapingNode);
        if (now > scrapingNode.expires)
        {
            // Token expired
            return new ScrapingNodeAuthenticationResult
            {
                tokenExpired = true,
                tokenAuthorized = false,
                tokenValid = true,
                msg = "Token expired. Contact ComputerElite if you want to help scraping.",
                scrapingNode = scrapingNode,
                overrideSettings = scrapingNodeOverrideSettings,
                compatibleScrapingVersion = OculusDBEnvironment.updater.version
            };
        }
        

        ScrapingNodeAuthenticationResult res = new ScrapingNodeAuthenticationResult
        {
            tokenExpired = false,
            tokenAuthorized = true,
            tokenValid = true,
            msg = "Token valid.",
            scrapingNode = scrapingNode,
            overrideSettings = scrapingNodeOverrideSettings,
            compatibleScrapingVersion = OculusDBEnvironment.updater.version
        };
        if (!res.scrapingNodeVersionCompatible)
        {
            // If scraping node version is incompatible scraping node is not allowed to scrape.
            res.tokenAuthorized = false;
            res.msg = "Scraping Node version is " + res.scrapingNode.scrapingNodeVersion + " but version " + res.compatibleScrapingVersion + " is needed. Please update your scraping node.";
        }

        return res;
    }

    public static void AddAppsToScrape(List<AppToScrape> appsToScrape, ScrapingNode scrapingNode)
    {
        // Add apps to be scraped
        // Only insert appsToScrape which ain't in appsToScrape already
        List<AppToScrape> appsToScrapeFiltered = new List<AppToScrape>();
        appsToScrape.ForEach(x =>
        {
            if (MongoDBInteractor.appsToScrape.Find(y => y.appId == x.appId).FirstOrDefault() == null)
            {
                x.currency = scrapingNode.currency;
                appsToScrapeFiltered.Add(x);
            }
        });
        MongoDBInteractor.appsToScrape.InsertMany(appsToScrapeFiltered);
        // Update scraping node stats
        ScrapingContribution contribution = new ScrapingContribution();
        contribution.scrapingNode = scrapingNode;
        contribution.taskResultsProcessed = 1;
        contribution.lastContribution = DateTime.UtcNow;
        contribution.appsQueuedForScraping += appsToScrapeFiltered.Count;
        IncScrapingNodeContribution(contribution);
    }

    public static void IncScrapingNodeContribution(ScrapingContribution scrapingContribution)
    {
        UpdateDefinition<ScrapingContribution> update = Builders<ScrapingContribution>.Update
            .Inc(x => x.appsQueuedForScraping, scrapingContribution.appsQueuedForScraping)
            .Inc(x => x.taskResultsProcessed, scrapingContribution.taskResultsProcessed);
        foreach (KeyValuePair<string,long> keyValuePair in scrapingContribution.contributionPerOculusDBType)
        {
            update = update.Inc(x => x.contributionPerOculusDBType[keyValuePair.Key], keyValuePair.Value);
        }

        scrapingNodeContributions.UpdateOne(
            x => x.scrapingNode.scrapingNodeId == scrapingContribution.scrapingNode.scrapingNodeId, update);
    }

    public static ScrapingContribution GetScrapingNodeContribution(ScrapingNode scrapingNode)
    {
        ScrapingContribution s = scrapingNodeContributions.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if (s == null)
        {
            s = new ScrapingContribution();
            s.scrapingNode = scrapingNode;
            scrapingNodeContributions.InsertOne(s);
        }
        s.scrapingNode = scrapingNode;
        return s;
    }
    
    public static List<string> existingScrapingNodes = new List<string>();

    public static ScrapingNodeStats GetScrapingNodeStats(ScrapingNode scrapingNode)
    {
        ScrapingNodeStats s = scrapingNodeStats.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if (s == null)
        {
            if (existingScrapingNodes.Contains(scrapingNode.scrapingNodeId))
            {
                // The entry should exist in the db. However as it doesn't let's return null so it doesn't get overriden with default data.
                return null;
            }
            s = new ScrapingNodeStats();
            s.scrapingNode = scrapingNode;
            scrapingNodeStats.InsertOne(s);
        }
        existingScrapingNodes.Add(scrapingNode.scrapingNodeId);
        s.scrapingNode = scrapingNode;
        return s;
    }

    public static void UpdateScrapingNodeStats(ScrapingNodeStats s)
    {
        if (s.snapshot.isPriorityScrape) return;
        if(DateTime.UtcNow < s.firstSight) s.firstSight = DateTime.UtcNow;
        s.SetOnline();
        scrapingNodeStats.DeleteMany(x => x.scrapingNode.scrapingNodeId == s.scrapingNode.scrapingNodeId);
        scrapingNodeStats.InsertOne(s);
    }

    public static List<DBVersion> versions = new ();
    public static void AddVersion(DBVersion? v, ref ScrapingContribution contribution)
    {
        if (v == null) return;
        contribution.AddContribution(v.__OculusDBType, 1);
        v.__sn = contribution.scrapingNode.scrapingNodeId;
        versions.Add(v);
    }

    public static List<DBIAPItem> iapItems = new ();
    public static void AddDLC(DBIAPItem? d, ref ScrapingContribution contribution)
    {
        if (d == null) return;
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        iapItems.Add(d);
    }

    public static List<DBIAPItemPack> dlcPacks = new ();
    public static void AddDLCPack(DBIAPItemPack? d, ref ScrapingContribution contribution)
    {
        if (d == null) return;
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        dlcPacks.Add(d);
    }
    
    public static List<DBApplication> apps = new ();
    public static void AddApplication(DBApplication? a, ref ScrapingContribution contribution)
    {
        if(a == null) return;
        contribution.AddContribution(a.__OculusDBType, 1);
        a.__sn = contribution.scrapingNode.scrapingNodeId;
        apps.Add(a);
    }

    public static List<BsonDocument> queuedActivity = new List<BsonDocument>();
    public static BsonDocument? AddBsonDocumentToActivityCollection(BsonDocument? d, ref ScrapingContribution contribution)
    {
        if (d == null) return null;
        contribution.AddContribution(d["__OculusDBType"].AsString, 1);
        d["__sn"] = contribution.scrapingNode.scrapingNodeId;
        queuedActivity.Add(d);
        return d;
    }

    public static List<ScrapingNodeStats> GetScrapingNodes()
    {
        return scrapingNodeStats.Find(x => true).SortBy(x => x.firstSight).ToList().ConvertAll(x =>
        {
            x.SetOnline();
            // Find contribution in DB
            x.contribution = scrapingNodeContributions.Find(y => y.scrapingNode.scrapingNodeId == x.scrapingNode.scrapingNodeId)
                .FirstOrDefault();
            if (x.contribution == null) x.contribution = new ScrapingContribution();
            if (ScrapingManaging.processingRn.TryGetValue(x.scrapingNode.scrapingNodeId, out ScrapingNodeTaskResultProcessing processing))
            {
                x.tasksProcessing = processing.processingCount;
            }
            return x;
        });
    }

    public static void CleanAppsScraping()
    {
        // Delete all apps which have been scraping for longer than 45 minutes
        appsScraping.DeleteMany(x => x.sentToScrapeTime < DateTime.UtcNow.AddMinutes(-60));
    }

    public static void AddAppToScrape(AppToScrape appToScrape, AppScrapePriority s = AppScrapePriority.Low)
    {
        appToScrape.scrapePriority = s;

        // check if app is scraping or already in queue as priority
        if (MongoDBInteractor.appsToScrape.Find(x => x.appId == appToScrape.appId && x.priority == appToScrape.priority).FirstOrDefault() != null
            || appsScraping.Find(x => x.appId == appToScrape.appId && x.priority == appToScrape.priority).FirstOrDefault() != null)
        {
            // App is already in queue
            return;
        }
        
        MongoDBInteractor.appsToScrape?.InsertOne(appToScrape);
    }

    public static List<DBAppImage> images = new ();
    public static void AddImage(DBAppImage? img, ref ScrapingContribution contribution)
    {
        if (img == null) return;
        contribution.AddContribution(img.__OculusDBType, 1);
        img.__sn = contribution.scrapingNode.scrapingNodeId;
        images.Add(img);
    }

    public static void Flush()
    {
        
    }

    public static string CreateScrapingNode(string id, string name)
    {
        ScrapingNode n = new ScrapingNode();
        n.scrapingNodeToken = RandomExtension.CreateToken();
        n.scrapingNodeId = id;
        n.scrapingNodeName = name;
        n.expires = DateTime.UtcNow.AddYears(10);
        scrapingNodes.InsertOne(n);
        return n.scrapingNodeToken;
    }

    public static ScrapingError AddErrorReport(ScrapingError error, ScrapingNodeAuthenticationResult r)
    {
        error.scrapingNodeId = r.scrapingNode.scrapingNodeId;
        scrapingErrors.InsertOne(error);
        return error;
    }

    public static ScrapingError GetErrorsReport(string id)
    {
        return scrapingErrors.Find(x => x._id == id).FirstOrDefault();
    }
}
