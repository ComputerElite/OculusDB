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
        DateTime now = DateTime.Now;
        
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
        DateTime now = DateTime.Now;
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
                }
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
                scrapingNode = scrapingNode
            };
        }
        

        ScrapingNodeAuthenticationResult res = new ScrapingNodeAuthenticationResult
        {
            tokenExpired = false,
            tokenAuthorized = true,
            tokenValid = true,
            msg = "Token valid.",
            scrapingNode = scrapingNode
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
        s.lastContribution = DateTime.Now;
        UpdateScrapingNodeStats(s);
    }

    public static ScrapingNodeStats GetScrapingNodeStats(ScrapingNode scrapingNode)
    {
        ScrapingNodeStats s = scrapingNodeStats.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if (s == null) s = new ScrapingNodeStats();
        s.scrapingNode = scrapingNode;
        return s;
    }

    public static void UpdateScrapingNodeStats(ScrapingNodeStats s)
    {
        if(DateTime.Now < s.firstSight) s.firstSight = DateTime.Now;
        scrapingNodeStats.DeleteOne(x => x.scrapingNode.scrapingNodeId == s.scrapingNode.scrapingNodeId);
        scrapingNodeStats.InsertOne(s);
    }

    public static void AddVersion(DBVersion v, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(v.__OculusDBType, 1);
        v.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        MongoDBInteractor.versionsCollection.DeleteOne(x => x.id == v.id);
        MongoDBInteractor.versionsCollection.InsertOne(v);
    }

    public static void AddDLC(DBIAPItem d, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        MongoDBInteractor.dlcCollection.DeleteOne(x => x.id == d.id);
        MongoDBInteractor.dlcCollection.InsertOne(d);
    }

    public static void AddDLCPack(DBIAPItemPack d, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        MongoDBInteractor.dlcPackCollection.DeleteOne(x => x.id == d.id);
        MongoDBInteractor.dlcPackCollection.InsertOne(d);
    }
    
    public static void AddApplication(DBApplication a, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(a.__OculusDBType, 1);
        a.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        AppToScrape t = MongoDBInteractor.appsScraping.FindOneAndDelete(x => x.appId == a.id);
        if (t != null)
        {
            MongoDBInteractor.scrapedApps.InsertOne(t);
        }
        MongoDBInteractor.applicationCollection.DeleteOne(x => x.id == a.id);
        MongoDBInteractor.applicationCollection.InsertOne(a);
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

    public static void AddImage(DBAppImage img, ref ScrapingNodeStats scrapingContribution)
    {
        scrapingContribution.contribution.AddContribution(img.__OculusDBType, 1);
        img.__sn = scrapingContribution.scrapingNode.scrapingNodeId;
        MongoDBInteractor.appImages.DeleteOne(x => x.appId == img.appId);
        MongoDBInteractor.appImages.InsertOne(img);
    }
}