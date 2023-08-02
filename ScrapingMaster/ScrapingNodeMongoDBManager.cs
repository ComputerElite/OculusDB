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
    }

    public static void CheckActivityCollection()
    {
        return;
        Logger.Log("Performing query...");
        List<string> ids = MongoDBInteractor.activityCollection.Distinct<string>("id",
            Builders<BsonDocument>.Filter.Eq("__OculusDBType", DBDataTypes.ActivityDLCUpdated)).ToList();
        Logger.Log("Id count: " + ids.Count);
        int i = 0;
        List<BsonDocument> toDelete = new List<BsonDocument>();
        foreach (string id in ids)
        {
            long count = MongoDBInteractor.activityCollection.CountDocuments(x => x["id"] == id);
            if (count >= 2)
            {
                List<BsonDocument> activities =
                    MongoDBInteractor.activityCollection.Find(x => x["id"] == id).ToList();
                activities = activities.OrderBy(x => x["__lastUpdated"].ToUniversalTime()).ToList();
                foreach (BsonDocument activity in activities)
                {
                    bool added = false;
                    if(activity["__OculusDBType"].AsString != "ActivityDLCUpdated") continue;
                    //Logger.Log(activity.ToJson());
                    if (activity["changeLog"].IsBsonNull)
                    {
                        toDelete.Add(activity);
                    }
                }
                Logger.Log("Deleting " + toDelete.Count + " activities of " + activities.Count + " activities");
            }

            i++;
            Logger.Log(i + "/" + ids.Count + "  (" + (i * 100.0 / ids.Count) + "%)");
            if (toDelete.Count >= 200)
            {
                Logger.Log("Deleting...");
                Logger.Log("deleted: " + MongoDBInteractor.activityCollection.DeleteMany(Builders<BsonDocument>.Filter.In("_id", toDelete.Select(x => x["_id"]))).DeletedCount);
                toDelete.Clear();
            }
        }
        Logger.Log("Deleting...");
        Logger.Log("deleted: " + MongoDBInteractor.activityCollection.DeleteMany(Builders<BsonDocument>.Filter.In("_id", toDelete.Select(x => x["_id"]))).DeletedCount);
        toDelete.Clear();
        Environment.Exit(0);
    }

    public static string GetRC(BsonDocument d)
    {
        return String.Join(',', d["releaseChannels"].AsBsonArray.Select(x => x["channel_name"].AsString).ToArray());
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
        if(DateTime.UtcNow < s.firstSight) s.firstSight = DateTime.UtcNow;
        s.SetOnline();
        scrapingNodeStats.DeleteMany(x => x.scrapingNode.scrapingNodeId == s.scrapingNode.scrapingNodeId);
        scrapingNodeStats.InsertOne(s);
    }

    public static List<DBVersion> versions = new ();
    public static void AddVersion(DBVersion v, ref ScrapingContribution contribution)
    {
        contribution.AddContribution(v.__OculusDBType, 1);
        v.__sn = contribution.scrapingNode.scrapingNodeId;
        versions.RemoveAll(x => x == null || x.id == v.id);
        if (v == null) return;
        versions.Add(v);
    }

    public static List<DBIAPItem> iapItems = new ();
    public static void AddDLC(DBIAPItem d, ref ScrapingContribution contribution)
    {
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        iapItems.Add(d);
    }

    public static List<DBIAPItemPack> dlcPacks = new ();
    public static void AddDLCPack(DBIAPItemPack d, ref ScrapingContribution contribution)
    {
        contribution.AddContribution(d.__OculusDBType, 1);
        d.__sn = contribution.scrapingNode.scrapingNodeId;
        dlcPacks.Add(d);
    }
    
    public static List<DBApplication> apps = new ();
    public static void AddApplication(DBApplication a, ref ScrapingContribution contribution)
    {
        contribution.AddContribution(a.__OculusDBType, 1);
        a.__sn = contribution.scrapingNode.scrapingNodeId;
        apps.Add(a);
    }

    public static List<BsonDocument> queuedActivity = new List<BsonDocument>();
    public static BsonDocument AddBsonDocumentToActivityCollection(BsonDocument d, ref ScrapingContribution contribution)
    {
        contribution.AddContribution(d["__OculusDBType"].AsString, 1);
        d["_id"] = ObjectId.GenerateNewId();
        d["__id"] = d["_id"].AsObjectId;
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

    public static void AddApp(AppToScrape appToScrape, AppScrapePriority s = AppScrapePriority.Low)
    {
        appToScrape.scrapePriority = s;

        // check if app is scraping or already in queue as priority
        if (MongoDBInteractor.appsToScrape.Find(x => x.appId == appToScrape.appId && x.priority == appToScrape.priority).FirstOrDefault() != null
            || appsScraping.Find(x => x.appId == appToScrape.appId && x.priority == appToScrape.priority).FirstOrDefault() != null)
        {
            // App is already in queue
            return;
        }
        
        MongoDBInteractor.appsToScrape.InsertOne(appToScrape);
    }

    public static List<DBAppImage> images = new ();
    public static void AddImage(DBAppImage img, ref ScrapingContribution contribution)
    {
        contribution.AddContribution(img.__OculusDBType, 1);
        img.__sn = contribution.scrapingNode.scrapingNodeId;
        images.Add(img);
    }

    public static TimeDependantBool flushing = new TimeDependantBool();
    public static void Flush()
    {
        if (flushing.IsTrueAndValid()) return;
        flushing.Set(true, TimeSpan.FromMinutes(2), "");
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
        
        List<string> addedIds = new ();
        while (iapItems.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = iapItems.Select(x => x.id).Take(Math.Min(iapItems.Count, 200)).ToArray();
            // Add to main DB for search
            MongoDBInteractor.dlcCollection.DeleteMany(x => ids.Contains(x.id));
            List<DBIAPItem> items = iapItems.Where(x => !addedIds.Contains(x.id)).Take(Math.Min(iapItems.Count, 200)).ToList();
            if (items.Count <= 0) break;
            MongoDBInteractor.dlcCollection.InsertMany(items);
            addedIds.AddRange(ids);
            
            // add to locale dlcs thing for future access to it
            foreach (DBIAPItem d in items)
            {
                GetLocaleDLCsCollection(d.current_offer.price.currency).DeleteMany(x => x.id == d.id);
                GetLocaleDLCsCollection(d.current_offer.price.currency).InsertOne(d);
            }
            iapItems.RemoveRange(0, Math.Min(iapItems.Count, 200));
        }
        

        Logger.Log("Adding " + dlcPacks.Count + " dlc packs to database.");
        addedIds.Clear();
        while (dlcPacks.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = dlcPacks.Select(x => x.id).Take(Math.Min(dlcPacks.Count, 200)).ToArray();
            // Add to main db for search
            MongoDBInteractor.dlcPackCollection.DeleteMany(x => ids.Contains(x.id));
            List<DBIAPItemPack> items = dlcPacks.Where(x => !addedIds.Contains(x.id)).Take(Math.Min(dlcPacks.Count, 200)).ToList();
            if (items.Count <= 0) break;
            MongoDBInteractor.dlcPackCollection.InsertMany(items);
            addedIds.AddRange(ids);
            
            // add to locale dlcs thing for future access to it
            foreach (DBIAPItemPack d in items)
            {
                GetLocaleDLCPacksCollection(d.current_offer.price.currency).DeleteMany(x => x.id == d.id);
                GetLocaleDLCPacksCollection(d.current_offer.price.currency).InsertOne(d);
            }
            
            dlcPacks.RemoveRange(0, Math.Min(dlcPacks.Count, 200));
        }
        
        Logger.Log("Adding " + apps.Count + " apps to database.");
        addedIds.Clear();
        while (apps.Count > 0)
        {
            // Bulk do work in batches of 200
            ids = apps.Select(x => x.id).Take(Math.Min(apps.Count, 200)).ToArray();
            // Add to main db for search
            MongoDBInteractor.applicationCollection.DeleteMany(x => ids.Contains(x.id));
            List<DBApplication> items = apps.Where(x => !addedIds.Contains(x.id)).Take(Math.Min(apps.Count, 200))
                .ToList();
            if (items.Count <= 0) break;
            MongoDBInteractor.applicationCollection.InsertMany(items);
            addedIds.AddRange(ids);
            
            // add to locale application thing for future access to it
            foreach (DBApplication a in items)
            {
                GetLocaleAppsCollection(a.currency).DeleteMany(x => x.id == a.id);
                GetLocaleAppsCollection(a.currency).InsertOne(a);
            }
            
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
        
        Logger.Log("Adding " + queuedActivity.Count + " activities to database and sending webhooks.");
        while (queuedActivity.Count > 0)
        {
            // Bulk do work in batches of 200
            List<BsonDocument> docs = queuedActivity.Take(Math.Min(queuedActivity.Count, 200)).ToList();
            MongoDBInteractor.activityCollection.InsertMany(docs);
            for(int i = 0; i < docs.Count; i++)
            {
                DiscordWebhookSender.SendActivity(docs[i]);
            }
            queuedActivity.RemoveRange(0, Math.Min(queuedActivity.Count, 200));
        }

        flushing.Set(false, TimeSpan.FromMinutes(0), "");
        CleanAppsScraping();
    }

    public static IMongoCollection<DBIAPItem> GetLocaleDLCsCollection(string currency)
    {
        return MongoDBInteractor.oculusDBDatabase.GetCollection<DBIAPItem>("dlcs-" + currency);
    }
    
    public static IMongoCollection<DBIAPItemPack> GetLocaleDLCPacksCollection(string currency)
    {
        return MongoDBInteractor.oculusDBDatabase.GetCollection<DBIAPItemPack>("dlcPacks-" + currency);
    }

    public static IMongoCollection<DBApplication> GetLocaleAppsCollection(string currency)
    {
        return MongoDBInteractor.oculusDBDatabase.GetCollection<DBApplication>("apps-" + currency);
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

    public static List<DBIAPItem> GetDLCs(string appId)
    {
        return MongoDBInteractor.dlcCollection.Find(x => x.parentApplication.id == appId).ToList();
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
