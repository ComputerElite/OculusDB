using ComputerUtils.Logging;
using ComputerUtils.RandomExtensions;
using MongoDB.Bson;
using MongoDB.Driver;
using OculusDB.Database;

namespace OculusDB.ScrapingMaster;

public class ScrapingNodeMongoDBManager
{
    public static IMongoClient mongoClient;
    public static IMongoDatabase oculusDBDatabase;
    public static IMongoCollection<ScrapingNode> scrapingNodes;
    public static IMongoCollection<ScrapingNodeStats> scrapingNodeStats;

    public static void Init()
    {
        mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
        oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
        scrapingNodes = oculusDBDatabase.GetCollection<ScrapingNode>("scrapingNodes");
        scrapingNodeStats = oculusDBDatabase.GetCollection<ScrapingNodeStats>("scrapingStats");
    }

    public static long GetNonPriorityAppsToScrapeCount()
    {
        return MongoDBInteractor.appsToScrape.CountDocuments(x => !x.priority);
    }

    public static List<AppToScrape> GetAppsToScrapeAndAddThemToScrapingApps(bool priority, int count, ScrapingNode responsibleForApps)
    {
        // Get apps to scrape
        List<AppToScrape> appsToScrape =
            MongoDBInteractor.appsToScrape.Find(x => x.priority == priority).SortByDescending(x => x.scrapePriority).Limit(count).ToList();
        // Set responsible scraping node, sent time and remove from apps to scrape
        DateTime now = DateTime.UtcNow;
        
        // Add apps to Scraping apps and remove them from apps to scrape
        if (appsToScrape.Count > 0)
        {
            appsToScrape.ForEach(x =>
            {
                x.responsibleScrapingNodeId = responsibleForApps.scrapingNodeId;
                x.sentToScrapeTime = now;
                MongoDBInteractor.appsToScrape.DeleteOne(y => y.appId == x.appId && y.priority == x.priority);
            });
            MongoDBInteractor.appsScraping.InsertMany(appsToScrape);
        }
        return appsToScrape;
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
                    scrapingNodeVersion = scrapingNodeIdentification.scrapingNodeVersion
                },
                compatibleScrapingVersion = OculusDBEnvironment.updater.version
            };
        }

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
        MongoDBInteractor.appsToScrape.InsertMany(appsToScrape);
        // Update scraping node stats
        ScrapingNodeStats s = GetScrapingNodeStats(scrapingNode);
        s.contribution.appsQueuedForScraping += appsToScrape.Count;
        s.lastContribution = DateTime.UtcNow;
        UpdateScrapingNodeStats(s);
    }

    public static ScrapingNodeStats GetScrapingNodeStats(ScrapingNode scrapingNode)
    {
        ScrapingNodeStats s = scrapingNodeStats.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if (s == null)
        {
            s = new ScrapingNodeStats();
            s.scrapingNode = scrapingNode;
            scrapingNodeStats.InsertOne(s);
        }
        s.scrapingNode = scrapingNode;
        return s;
    }

    public static void UpdateScrapingNodeStats(ScrapingNodeStats s)
    {
        if(DateTime.UtcNow < s.firstSight) s.firstSight = DateTime.UtcNow;
        s.SetOnline();
        scrapingNodeStats.ReplaceOne(x => x.scrapingNode.scrapingNodeId == s.scrapingNode.scrapingNodeId, s);
    }

    public static List<DBVersion> versions = new ();
    public static void AddVersion(DBVersion v, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(v.__OculusDBType, 1);
        v.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        versions.Add(v);
    }

    public static List<DBIAPItem> iapItems = new ();
    public static void AddDLC(DBIAPItem d, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        iapItems.Add(d);
    }

    public static List<DBIAPItemPack> dlcPacks = new ();
    public static void AddDLCPack(DBIAPItemPack d, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        dlcPacks.Add(d);
    }
    
    public static List<DBApplication> apps = new ();
    public static void AddApplication(DBApplication a, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(a.__OculusDBType, 1);
        a.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        AppToScrape t = MongoDBInteractor.appsScraping.FindOneAndDelete(x => x.appId == a.id);
        if (t != null)
        {
            MongoDBInteractor.scrapedApps.InsertOne(t);
        }
        apps.Add(a);
    }
    
    public static BsonDocument AddBsonDocumentToActivityCollection(BsonDocument d, ScrapingNode scrapingNode)
    {
        d["_id"] = ObjectId.GenerateNewId();
        d["__sn"] = scrapingNode.scrapingNodeId;
        MongoDBInteractor.activityCollection.InsertOne(d);
        return MongoDBInteractor.activityCollection.Find(x => x["_id"] == d["_id"]).FirstOrDefault();
    }

    public static List<ScrapingNodeStats> GetScrapingNodes()
    {
        return scrapingNodeStats.Find(x => true).ToList();
    }


    public static void AddApp(AppToScrape appToScrape, AppScrapePriority s = AppScrapePriority.Low)
    {
        appToScrape.scrapePriority = s;
    }

    public static List<DBAppImage> images = new ();
    public static void AddImage(DBAppImage img, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(img.__OculusDBType, 1);
        img.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        images.Add(img);
    }

    public static void Flush()
    {
        Logger.Log("Adding " + versions.Count + " versions to database.");
        string[] ids = new string[200];
        while (versions.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = versions.Select(x => x.id).Take(Math.Min(versions.Count, 200)).ToArray();
            MongoDBInteractor.versionsCollection.DeleteMany(x => ids.Contains(x.id));
            MongoDBInteractor.versionsCollection.InsertMany(versions.Take(Math.Min(versions.Count, 200)));
            versions.RemoveRange(0, Math.Min(versions.Count, 200));
        }
        

        Logger.Log("Adding " + iapItems.Count + " dlcs to database.");
        while (iapItems.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = iapItems.Select(x => x.id).Take(Math.Min(iapItems.Count, 200)).ToArray();
            MongoDBInteractor.dlcCollection.DeleteMany(x => ids.Contains(x.id));
            MongoDBInteractor.dlcCollection.InsertMany(iapItems.Take(Math.Min(iapItems.Count, 200)));
            iapItems.RemoveRange(0, Math.Min(iapItems.Count, 200));
        }
        

        Logger.Log("Adding " + dlcPacks.Count + " dlc packs to database.");
        while (dlcPacks.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = dlcPacks.Select(x => x.id).Take(Math.Min(dlcPacks.Count, 200)).ToArray();
            MongoDBInteractor.dlcPackCollection.DeleteMany(x => ids.Contains(x.id));
            MongoDBInteractor.dlcPackCollection.InsertMany(dlcPacks.Take(Math.Min(dlcPacks.Count, 200)));
            dlcPacks.RemoveRange(0, Math.Min(dlcPacks.Count, 200));
        }
        
        Logger.Log("Adding " + apps.Count + " apps to database.");
        while (apps.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = apps.Select(x => x.id).Take(Math.Min(apps.Count, 200)).ToArray();
            MongoDBInteractor.applicationCollection.DeleteMany(x => ids.Contains(x.id));
            MongoDBInteractor.applicationCollection.InsertMany(apps.Take(Math.Min(apps.Count, 200)));
            apps.RemoveRange(0, Math.Min(apps.Count, 200));
        }
        
        Logger.Log("Adding " + images.Count + " images to database.");
        while (images.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = images.Select(x => x.appId).Take(Math.Min(images.Count, 200)).ToArray();
            MongoDBInteractor.appImages.DeleteMany(x => ids.Contains(x.appId));
            MongoDBInteractor.appImages.InsertMany(images.Take(Math.Min(images.Count, 200)));
            images.RemoveRange(0, Math.Min(images.Count, 200));
        }
    }

    public static string CreateScrapingNode(string id, string? name)
    {
        ScrapingNode n = new ScrapingNode();
        n.scrapingNodeToken = RandomExtension.CreateToken();
        n.scrapingNodeId = id;
        n.scrapingNodeName = name;
        n.expires = DateTime.UtcNow.AddYears(10);
        scrapingNodes.InsertOne(n);
        return n.scrapingNodeToken;
    }
}