using MongoDB.Driver;

namespace OculusDB.ScrapingMaster;

public class ScrapingNodeMongoDBManager
{
    public static IMongoClient mongoClient;
    public static IMongoDatabase oculusDBDatabase;
    public static IMongoCollection<ScrapingNode> scrapingNodes;
    public static IMongoCollection<ScrapingContribution> scrapingContributions;

    public static void Init()
    {
        mongoClient = new MongoClient(OculusDBEnvironment.config.mongoDBUrl);
        oculusDBDatabase = mongoClient.GetDatabase(OculusDBEnvironment.config.mongoDBName);
        scrapingNodes = oculusDBDatabase.GetCollection<ScrapingNode>("scrapingNodes");
        scrapingContributions = oculusDBDatabase.GetCollection<ScrapingContribution>("scrapingContributions");
        
        MongoDBInteractor.RemoveIdRemap<ScrapingNode>();
    }

    public static long GetNonPriorityAppsToScrapeCount()
    {
        return MongoDBInteractor.appsToScrape.CountDocuments(x => !x.priority);
    }

    public static List<AppToScrape> GetAppsToScrapeAndAddThemToScrapingApps(bool priority, int count, ScrapingNode responsibleForApps)
    {
        // Get apps to scrape
        List<AppToScrape> appsToScrape =
            MongoDBInteractor.appsToScrape.Find(x => x.priority == priority).Limit(count).ToList();
        // Set responsible scraping node, sent time and remove from apps to scrape
        DateTime now = DateTime.Now;
        appsToScrape.ForEach(x =>
        {
            x.responsibleScrapingNodeId = responsibleForApps.scrapingNodeId;
            x.sentToScrapeTime = now;
            MongoDBInteractor.appsToScrape.DeleteOne(y => y.appId == x.appId && y.priority == x.priority);
        });
        
        // Add apps to Scraping apps
        MongoDBInteractor.appsScraping.InsertMany(appsToScrape);
        return appsToScrape;
    }
    
    public static ScrapingNodeAuthenticationResult CheckScrapingNode(ScrapingNodeIdentification scrapingNodeIdentification)
    {
        DateTime now = DateTime.Now;;
        ScrapingNode scrapingNode = scrapingNodes.Find(x => x.scrapingNodeToken == scrapingNodeIdentification.scrapingNodeToken).FirstOrDefault();
        if (scrapingNode == null)
        {
            // Token not found
            return new ScrapingNodeAuthenticationResult
            {
                tokenExpired = false,
                tokenAuthorized = false,
                tokenValid = false,
                msg = "Token not found. Contact ComputerElite if you want to help scraping."
            };
        }
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

        return new ScrapingNodeAuthenticationResult
        {
            tokenExpired = false,
            tokenAuthorized = true,
            tokenValid = true,
            msg = "Token valid.",
            scrapingNode = scrapingNode
        };
    }
}