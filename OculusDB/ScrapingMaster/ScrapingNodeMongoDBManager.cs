using ComputerUtils.Logging;
using ComputerUtils.RandomExtensions;
using MongoDB.Bson;
using MongoDB.Driver;
using OculusDB.Database;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusDB.Users;

namespace OculusDB.ScrapingMaster;

public class ScrapingNodeMongoDBManager
{
    public static void Init()
    {
        OculusDBDatabase.Initialize();

        CleanAppsScraping();
    }

    public static void AddScrapingProcessingStat(ScrapingProcessingStats scrapingProcessingStat)
    {
        OculusDBDatabase.scrapingProcessingStats.InsertOne(scrapingProcessingStat);
    }

    public static List<ScrapingProcessingStats> GetScrapingProcessingStats()
    {
        return OculusDBDatabase.scrapingProcessingStats.Find(x => true).Limit(1000).ToList();
    }

    public static long GetNonPriorityAppsToScrapeCount(string currency)
    {
        return OculusDBDatabase.appsToScrape.CountDocuments(x => !x.priority && x.currency == currency);
    }

    public static List<AppToScrape> GetAppsToScrapeAndAddThemToScrapingApps(bool priority, int count, ScrapingNode responsibleForApps)
    {
        // Get apps to scrape
        string currency = responsibleForApps.currency;
        List<AppToScrape> appsToScrape =
            OculusDBDatabase.appsToScrape.Find(x => x.priority == priority && (x.currency == currency || x.currency == "")).SortByDescending(x => x.scrapePriority).Limit(count).ToList();
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
                OculusDBDatabase.appsScraping.DeleteMany(y => y.appId == x.appId && y.priority == x.priority && y.currency == x.currency);
                OculusDBDatabase.appsScraping.InsertOne(x);
                if(!selected.Any(y => x.appId == y.appId && x.priority == y.priority))
                    selected.Add(x);
                OculusDBDatabase.appsToScrape.DeleteOne(y => y.appId == x.appId && y.priority == x.priority && y.currency == x.currency);
            });
        }
        
        return selected;
    }

    public static void RemoveAppFromAppsScraping(DBApplication app)
    {
        OculusDBDatabase.appsScraping.DeleteOne(x => x.appId == app.id);
    }
    
    public static ScrapingNodeAuthenticationResult CheckScrapingNode(ScrapingNodeIdentification scrapingNodeIdentification)
    {
        DateTime now = DateTime.UtcNow;
        ScrapingNode scrapingNode = OculusDBDatabase.scrapingNodes.Find(x => x.scrapingNodeToken == scrapingNodeIdentification.scrapingNodeToken).FirstOrDefault();
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
        ScrapingNodeOverrideSettings scrapingNodeOverrideSettings = OculusDBDatabase.scrapingNodeOverrideSettingses.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if(scrapingNodeOverrideSettings == null) scrapingNodeOverrideSettings = new ScrapingNodeOverrideSettings();
        
        scrapingNode.currency = scrapingNodeIdentification.currency;
        scrapingNode.scrapingNodeVersion = scrapingNodeIdentification.scrapingNodeVersion;
        OculusDBDatabase.scrapingNodes.ReplaceOne(x => x.scrapingNodeToken == scrapingNodeIdentification.scrapingNodeToken, scrapingNode);
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
            if (OculusDBDatabase.appsToScrape.Find(y => y.appId == x.appId).FirstOrDefault() == null)
            {
                x.currency = scrapingNode.currency;
                appsToScrapeFiltered.Add(x);
            }
        });
        OculusDBDatabase.appsToScrape.InsertMany(appsToScrapeFiltered);
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

        OculusDBDatabase.scrapingNodeContributions.UpdateOne(
            x => x.scrapingNode.scrapingNodeId == scrapingContribution.scrapingNode.scrapingNodeId, update);
    }

    public static ScrapingContribution GetScrapingNodeContribution(ScrapingNode scrapingNode)
    {
        ScrapingContribution s = OculusDBDatabase.scrapingNodeContributions.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if (s == null)
        {
            s = new ScrapingContribution();
            s.scrapingNode = scrapingNode;
            OculusDBDatabase.scrapingNodeContributions.InsertOne(s);
        }
        s.scrapingNode = scrapingNode;
        return s;
    }
    
    public static List<string> existingScrapingNodes = new List<string>();

    public static ScrapingNodeStats GetScrapingNodeStats(ScrapingNode scrapingNode)
    {
        ScrapingNodeStats s = OculusDBDatabase.scrapingNodeStats.Find(x => x.scrapingNode.scrapingNodeId == scrapingNode.scrapingNodeId).FirstOrDefault();
        if (s == null)
        {
            if (existingScrapingNodes.Contains(scrapingNode.scrapingNodeId))
            {
                // The entry should exist in the db. However as it doesn't let's return null so it doesn't get overriden with default data.
                return null;
            }
            s = new ScrapingNodeStats();
            s.scrapingNode = scrapingNode;
            OculusDBDatabase.scrapingNodeStats.InsertOne(s);
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
        OculusDBDatabase.scrapingNodeStats.DeleteMany(x => x.scrapingNode.scrapingNodeId == s.scrapingNode.scrapingNodeId);
        OculusDBDatabase.scrapingNodeStats.InsertOne(s);
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
    public static void AddIapItem(DBIAPItem? d, ref ScrapingContribution contribution)
    {
        if (d == null) return;
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        iapItems.Add(d);
    }

    public static List<DBIAPItemPack> dlcPacks = new ();
    public static void AddIapItemPack(DBIAPItemPack? d, ref ScrapingContribution contribution)
    {
        if (d == null) return;
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        dlcPacks.Add(d);
    }
    public static List<DBOffer> offers = new ();
    public static void AddOffer(DBOffer? d, ref ScrapingContribution contribution)
    {
        if (d == null) return;
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        offers.Add(d);
    }
    public static List<DBAchievement> achievements = new ();
    public static void AddAchievement(DBAchievement? d, ref ScrapingContribution contribution)
    {
        if (d == null) return;
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        achievements.Add(d);
    }
    
    public static List<DBApplication> apps = new ();
    public static void AddApplication(DBApplication? a, ref ScrapingContribution contribution)
    {
        if(a == null) return;
        contribution.AddContribution(a.__OculusDBType, 1);
        apps.Add(a);
    }

    public static List<DBDifference> differences = new List<DBDifference>();
    public static void AddDiff(DBDifference? d, ref ScrapingContribution contribution)
    {
        if (d == null || d.isSame) return;
        contribution.AddContribution(d.__OculusDBType, 1);
        differences.Add(d);
    }

    public static List<ScrapingNodeStats> GetScrapingNodes()
    {
        return OculusDBDatabase.scrapingNodeStats.Find(x => true).SortBy(x => x.firstSight).ToList().ConvertAll(x =>
        {
            x.SetOnline();
            // Find contribution in DB
            x.contribution = OculusDBDatabase.scrapingNodeContributions.Find(y => y.scrapingNode.scrapingNodeId == x.scrapingNode.scrapingNodeId)
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
        OculusDBDatabase.appsScraping.DeleteMany(x => x.sentToScrapeTime < DateTime.UtcNow.AddMinutes(-60));
    }

    public static void AddAppToScrape(AppToScrape appToScrape, AppScrapePriority s = AppScrapePriority.Low)
    {
        if (appToScrape.appId == "") return;
        appToScrape.scrapePriority = s;

        // check if app is scraping or already in queue as priority
        if (OculusDBDatabase.appsToScrape.Find(x => x.appId == appToScrape.appId && x.priority == appToScrape.priority).FirstOrDefault() != null
            || OculusDBDatabase.appsScraping.Find(x => x.appId == appToScrape.appId && x.priority == appToScrape.priority).FirstOrDefault() != null)
        {
            // App is already in queue
            return;
        }
        
        OculusDBDatabase.appsToScrape?.InsertOne(appToScrape);
    }

    public static List<DBAppImage> images = new ();

    public static void AddImage(DBAppImage? img, ref ScrapingContribution contribution)
    {
        if (img == null) return;
        contribution.AddContribution(img.__OculusDBType, 1);
        img.__sn = contribution.scrapingNode.scrapingNodeId;
        images.Add(img);
    }
    
    public static Dictionary<string, TimeDependantBool> lockers = new Dictionary<string, TimeDependantBool>();
    

    /// <summary>
    /// Inserts all provided items into the provided collection
    /// </summary>
    /// <param name="collection">Collection to insert to</param>
    /// <param name="items">items to insert</param>
    /// <typeparam name="T">Type of the entries</typeparam>
    public static void BulkInsert<T>(IMongoCollection<T>? collection, ref List<T> items) where T : IDBObjectOperations<T>
    {
        if (collection == null) throw new Exception("Collection is null");
        string typeName = typeof(T).Name;
        // only allow one Thread to write to a given collection at once
        if(!lockers.ContainsKey(typeName)) lockers.Add(typeName, new TimeDependantBool());
        if (lockers[typeName].IsTrueAndValid()) return;
        lockers[typeName].Set(true, TimeSpan.FromMinutes(30));
        
        while (items.Count > 0)
        {
            // Add all items with the same id to a list
            items[0].AddOrUpdateEntry(collection);
            items.RemoveAt(0);
        }
        // Unlock this type to be able to write to it again
        lockers[typeName].Unlock();
    }

    /// <summary>
    /// Writes all cached entries to the Database
    /// </summary>
    public static void Flush()
    {
        BulkInsert(OculusDBDatabase.applicationCollection, ref apps);
        BulkInsert(OculusDBDatabase.versionCollection, ref versions);
        BulkInsert(OculusDBDatabase.iapItemCollection, ref iapItems);
        BulkInsert(OculusDBDatabase.iapItemPackCollection, ref dlcPacks);
        BulkInsert(OculusDBDatabase.offerCollection, ref offers);
        BulkInsert(OculusDBDatabase.achievementCollection, ref achievements);
        BulkInsert(OculusDBDatabase.appImages, ref images);
        List<DBDifference> diffs = new List<DBDifference>(differences);
        differences.Clear();
        if(diffs.Count > 0) OculusDBDatabase.differenceCollection.InsertMany(diffs);
        //BulkInsert(OculusDBDatabase.differenceCollection, ref differences);
    }

    public static string CreateScrapingNode(string id, string name)
    {
        ScrapingNode n = new ScrapingNode();
        n.scrapingNodeToken = RandomExtension.CreateToken();
        n.scrapingNodeId = id;
        n.scrapingNodeName = name;
        n.expires = DateTime.UtcNow.AddYears(10);
        OculusDBDatabase.scrapingNodes.InsertOne(n);
        return n.scrapingNodeToken;
    }

    public static ScrapingError AddErrorReport(ScrapingError error, ScrapingNodeAuthenticationResult r)
    {
        error.scrapingNodeId = r.scrapingNode.scrapingNodeId;
        OculusDBDatabase.scrapingErrors.InsertOne(error);
        return error;
    }

    public static ScrapingError GetErrorsReport(string id)
    {
        return OculusDBDatabase.scrapingErrors.Find(x => x._id == id).FirstOrDefault();
    }
}
