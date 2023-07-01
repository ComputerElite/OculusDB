using System.Net;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusDB.ScrapingNodeCode;
using OculusDB.Users;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;

namespace OculusDB.ScrapingMaster;

public class ScrapingManaging
{
    public static TimeDependantBool isAppAddingRunning { get; set; } = new TimeDependantBool();
    public static Dictionary<string, ScrapingNodeTaskResultProcessing> processingRn = new ();
    public static List<ScrapingTask> GetTasks(ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {
        if (ScrapingNodeMongoDBManager.GetNonPriorityAppsToScrapeCount() <= 20)
        {
            // Apps to scrape should be added
            if (!isAppAddingRunning)
            {
                // Request node to get all apps on Oculus for scraping. Results should be returned within 10 minutes.
                isAppAddingRunning.Set(true, TimeSpan.FromMinutes(10), scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId);
                return new List<ScrapingTask>
                {
                    new()
                    {
                        scrapingTask = ScrapingTaskType.GetAllAppsToScrape
                    }
                };
            }
            if (isAppAddingRunning.IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
            {
                // Why tf are you requesting tasks. You should be getting all apps to scrape rn you idiot
                return new List<ScrapingTask>
                {
                    new()
                    {
                        scrapingTask = ScrapingTaskType.GetAllAppsToScrape
                    }
                };
            }
        }
        
        // There are enough apps to scrape or app adding is running. Send scraping tasks.
        List<ScrapingTask> scrapingTasks = new();
        // Add 20 non-priority apps to scrape and 3 priority apps to scrape
        scrapingTasks.AddRange(ConvertAppsToScrapeToScrapingTasks(ScrapingNodeMongoDBManager.GetAppsToScrapeAndAddThemToScrapingApps(false, 20, scrapingNodeAuthenticationResult.scrapingNode)));
        scrapingTasks.AddRange(ConvertAppsToScrapeToScrapingTasks(ScrapingNodeMongoDBManager.GetAppsToScrapeAndAddThemToScrapingApps(true, 3, scrapingNodeAuthenticationResult.scrapingNode)));

        return scrapingTasks;
    }

    public static List<ScrapingTask> ConvertAppsToScrapeToScrapingTasks(List<AppToScrape> apps)
    {
        return apps.ConvertAll<ScrapingTask>(
            x =>
            {
                ScrapingTask task = new ScrapingTask();
                task.appToScrape = x;
                task.scrapingTask = ScrapingTaskType.ScrapeApp;
                return task;
            });
    }

    public static ScrapingProcessedResult ProcessTaskResult(ScrapingNodeTaskResult taskResult, ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {
        if (!processingRn.ContainsKey(scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId))
            processingRn.Add(scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId,
                new ScrapingNodeTaskResultProcessing());
        processingRn[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId].Start();
        
        ScrapingProcessedResult r = new ScrapingProcessedResult();
        Logger.Log("Results of Scraping node " + scrapingNodeAuthenticationResult.scrapingNode + " received. Processing now...");
        Logger.Log("Result type: " +
                   Enum.GetName(typeof(ScrapingNodeTaskResultType), taskResult.scrapingNodeTaskResultType));
        // Process results of scraping:
        //   - When apps for scraping have been sent, add them to the DB for scraping
        //   - On error while requesting apps to scrape, make other scraping nodes able to request apps to scrape
        //   - When scraping is done, compute the activity entries and write both to the DB (Each scraped app should)
        switch (taskResult.scrapingNodeTaskResultType)
        {
            case ScrapingNodeTaskResultType.Unknown:
                r.processed = false;
                r.msg = "Cannot process unknown task result type.";
                processingRn[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId].Done();
                return r;
            case ScrapingNodeTaskResultType.ErrorWhileRequestingAppsToScrape:
                if (!isAppAddingRunning.IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
                {
                    Logger.Log("Node is not responsible for adding apps to scrape. Ignoring error.");
                    r.processed = false;
                    r.msg = "You are not responsible for adding apps to scrape. Your submission has been ignored.";
                    r.failedCount = 1;
                    processingRn[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId].Done();
                    return r;
                }
                Logger.Log("Error while requesting apps to scrape. Making other scraping nodes able to request apps to scrape.");
                isAppAddingRunning.Set(false, TimeSpan.FromMinutes(10), "");
                break;
            case ScrapingNodeTaskResultType.FoundAppsToScrape:
                if (!isAppAddingRunning.IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
                {
                    Logger.Log("Node is not responsible for adding apps to scrape. Ignoring.");
                    r.processed = false;
                    r.msg = "You are not responsible for adding apps to scrape. Your submission has been ignored.";
                    r.failedCount = 1;
                    processingRn[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId].Done();
                    return r;
                }
                Logger.Log("Found apps to scrape. Adding them to the DB.");
                ScrapingNodeMongoDBManager.AddAppsToScrape(taskResult.appsToScrape, scrapingNodeAuthenticationResult.scrapingNode);
                r.processed = true;
                r.processedCount = taskResult.appsToScrape.Count;
                r.msg = "Added " + taskResult.appsToScrape.Count + " apps to scrape. Thanks for the cooperation.";
                break;
            case ScrapingNodeTaskResultType.AppsScraped:
                ProcessScrapedResults(taskResult, scrapingNodeAuthenticationResult, ref r);
                r.msg = "Processed " + taskResult.scraped.applications.Count + " applications, " + taskResult.scraped.dlcs.Count + " dlcs, " + taskResult.scraped.dlcPacks.Count + " dlc packs, " + taskResult.scraped.versions.Count + " version and " + taskResult.scraped.imgs.Count + " images from scraping node " + scrapingNodeAuthenticationResult.scrapingNode + ". Thanks for your contribution.";
                break;
        }
        processingRn[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId].Done();
        return r;
    }

    private static void ProcessScrapedResults(ScrapingNodeTaskResult taskResult, ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult, ref ScrapingProcessedResult r)
    {
        Logger.Log("Processing " + taskResult.scraped.applications.Count + " applications, " + taskResult.scraped.dlcs.Count + " dlcs, " + taskResult.scraped.dlcPacks.Count + " dlc packs, " + taskResult.scraped.versions.Count + " version and " + taskResult.scraped.imgs.Count + " images from scraping node " + scrapingNodeAuthenticationResult.scrapingNode);
        ScrapingNodeStats scrapingContribution = ScrapingNodeMongoDBManager.GetScrapingNodeStats(scrapingNodeAuthenticationResult.scrapingNode);
        // Process Versions
        foreach (DBVersion v in taskResult.scraped.versions)
        {
            DBApplication parentApplication =
                taskResult.scraped.applications.FirstOrDefault(x => x.id == v.parentApplication.id);
            if (parentApplication == null)
            {
                Logger.Log("Skipping " + v + " because the parent application isn't in the scraping results");
                continue;
            }
            BsonDocument lastActivity = MongoDBInteractor.GetLastEventWithIDInDatabaseVersion(v.id);
            DBVersion oldEntry = ObjectConverter.ConvertToDBType(MongoDBInteractor.GetByID(v.id).FirstOrDefault());
            
            // Create activity entry
            DBActivityNewVersion newVersion = new DBActivityNewVersion();
            newVersion.id = v.id;
            newVersion.changeLog = v.changeLog;
            newVersion.parentApplication.id = parentApplication.id;
            newVersion.parentApplication.hmd = parentApplication.hmd;
            newVersion.parentApplication.canonicalName = parentApplication.canonicalName;
            newVersion.parentApplication.displayName = parentApplication.displayName;
            newVersion.releaseChannels = v.binary_release_channels.nodes;
            newVersion.version = v.version;
            newVersion.versionCode = v.versionCode;
            newVersion.uploadedTime = TimeConverter.UnixTimeStampToDateTime(v.created_date);
            
            // Changelog updated
            if(v.changeLog != "" && v.changeLog != null)
            {
				DBActivityVersionChangelogAvailable e = ObjectConverter.ConvertCopy<DBActivityVersionChangelogAvailable, DBActivityNewVersion>(newVersion);
                if (oldEntry == null || oldEntry.changeLog == "")
				{
					// Changelog is most likely new
					e.__OculusDBType = DBDataTypes.ActivityVersionChangelogAvailable;
					DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(e.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
				}
				else if(oldEntry != null && oldEntry.changeLog != v.changeLog)
				{
					// Changelog got most likely updated
					e.__OculusDBType = DBDataTypes.ActivityVersionChangelogUpdated;
					DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(ObjectConverter.ConvertCopy<DBActivityVersionChangelogUpdated, DBActivityVersionChangelogAvailable>(e).ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
				}
			}

			if (lastActivity == null)
            {
                DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(newVersion.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
            }
            else
            {
                DBActivityVersionUpdated oldUpdate = lastActivity["__OculusDBType"] == DBDataTypes.ActivityNewVersion ? ObjectConverter.ConvertCopy<DBActivityVersionUpdated, DBActivityNewVersion>(ObjectConverter.ConvertToDBType(lastActivity)) : ObjectConverter.ConvertToDBType(lastActivity);
                if (oldUpdate.changeLog != newVersion.changeLog && newVersion.changeLog != null && newVersion.changeLog != "" || String.Join(',', oldUpdate.releaseChannels.Select(x => x.channel_name).ToList()) != String.Join(',', newVersion.releaseChannels.Select(x => x.channel_name).ToList()))
                {
                    DBActivityVersionUpdated toAdd = ObjectConverter.ConvertCopy<DBActivityVersionUpdated, DBActivityNewVersion>(newVersion);
                    toAdd.__OculusDBType = DBDataTypes.ActivityVersionUpdated;
                    toAdd.__lastEntry = lastActivity["_id"].ToString();
                    DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(toAdd.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
                }
            }
            // Update contributions
            ScrapingNodeMongoDBManager.AddVersion(v, ref scrapingContribution);
            r.processedCount++;
        }
        
        // Process DLCs
        foreach (DBIAPItem d in taskResult.scraped.dlcs)
        {
            DBApplication parentApplication =
                taskResult.scraped.applications.FirstOrDefault(x => x.id == d.parentApplication.id);
            if (parentApplication == null)
            {
                Logger.Log("Skipping " + d + " because the parent application isn't in the scraping results");
                continue;
            }
            DBActivityNewDLC newDLC = new DBActivityNewDLC();
            newDLC.id = d.id;
            newDLC.parentApplication.id = parentApplication.id;
            newDLC.parentApplication.hmd = parentApplication.hmd;
            newDLC.parentApplication.canonicalName = parentApplication.canonicalName;
            newDLC.parentApplication.displayName = parentApplication.displayName;
            newDLC.displayName = d.display_name;
            newDLC.displayShortDescription = d.display_short_description;
            newDLC.latestAssetFileId = d.latest_supported_asset_file != null ? d.latest_supported_asset_file.id : "";
            newDLC.priceOffset = d.current_offer.price.offset_amount;
            
            BsonDocument oldDLC = MongoDBInteractor.GetLastEventWithIDInDatabase(d.id);
            ScrapingNodeMongoDBManager.AddDLC(d, ref scrapingContribution);
            if (oldDLC == null)
            {
                DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(newDLC.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
            }
            else if (oldDLC["latestAssetFileId"] != newDLC.latestAssetFileId || oldDLC["priceOffset"] != newDLC.priceOffset || oldDLC["displayName"] != newDLC.displayName || oldDLC["displayShortDescription"] != newDLC.displayShortDescription)
            {
                DBActivityDLCUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCUpdated, DBActivityNewDLC>(newDLC);
                updated.__lastEntry = oldDLC["_id"].ToString();
                updated.__OculusDBType = DBDataTypes.ActivityDLCUpdated;
                DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(updated.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
            }
            r.processedCount++;
        }
        
        // Process DLC Packs
        foreach (DBIAPItemPack d in taskResult.scraped.dlcPacks)
        {
            DBApplication parentApplication =
                taskResult.scraped.applications.FirstOrDefault(x => x.id == d.parentApplication.id);
            if (parentApplication == null)
            {
                Logger.Log("Skipping " + d + " because the parent application isn't in the scraping results");
                continue;
            }
            DBActivityNewDLCPack newDLCPack = new DBActivityNewDLCPack();
            newDLCPack.id = d.id;
            newDLCPack.parentApplication.id = parentApplication.id;
            newDLCPack.parentApplication.hmd = parentApplication.hmd;
            newDLCPack.parentApplication.canonicalName = parentApplication.canonicalName;
            newDLCPack.parentApplication.displayName = parentApplication.displayName;
            newDLCPack.displayName = d.display_name;
            newDLCPack.displayShortDescription = d.display_short_description;
            newDLCPack.priceOffset = d.current_offer.price.offset_amount;
            
            BsonDocument oldDLC = MongoDBInteractor.GetLastEventWithIDInDatabase(d.id);
            ScrapingNodeMongoDBManager.AddDLCPack(d, ref scrapingContribution);
            newDLCPack.__OculusDBType = DBDataTypes.ActivityNewDLCPack;
            foreach (DBItemId item in d.bundle_items)
            {
                DBIAPItem matching = taskResult.scraped.dlcs.FirstOrDefault(x => x.id == item.id);
                if(matching == null) continue;
                if (matching == null) continue;
                DBActivityNewDLCPackDLC dlcItem = new DBActivityNewDLCPackDLC();
                dlcItem.id = matching.id;
                dlcItem.displayName = matching.display_name;
                dlcItem.displayShortDescription = matching.display_short_description;
                newDLCPack.includedDLCs.Add(dlcItem);
            }
            if (oldDLC == null)
            {
                DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(newDLCPack.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
            }
            else if (oldDLC["priceOffset"] != newDLCPack.priceOffset || oldDLC["displayName"] != newDLCPack.displayName || oldDLC["displayShortDescription"] != newDLCPack.displayShortDescription || String.Join(',', BsonSerializer.Deserialize<DBActivityNewDLCPack>(oldDLC).includedDLCs.Select(x => x.id).ToList()) != String.Join(',', newDLCPack.includedDLCs.Select(x => x.id).ToList()))
            {
                DBActivityDLCPackUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCPackUpdated, DBActivityNewDLCPack>(newDLCPack);
                updated.__lastEntry = oldDLC["_id"].ToString();
                updated.__OculusDBType = DBDataTypes.ActivityDLCPackUpdated;
                DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(updated.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
            }
            r.processedCount++;
        }
        
        // Process Applications
        foreach (DBApplication a in taskResult.scraped.applications)
        {
            // New Application activity
            if (MongoDBInteractor.GetLastEventWithIDInDatabase(a.id) == null)
            {
                DBActivityNewApplication e = new DBActivityNewApplication();
                e.id = a.id;
                e.hmd = a.hmd;
                e.publisherName = a.publisher_name;
                e.displayName = a.displayName;
                e.priceOffsetNumerical = a.priceOffsetNumerical;
                e.priceFormatted = a.priceFormatted;
                e.displayLongDescription = a.display_long_description;
                e.releaseDate = TimeConverter.UnixTimeStampToDateTime(a.release_date);
                e.supportedHmdPlatforms = a.supported_hmd_platforms;
                DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(e.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
            }
            
            
            // Price Change activity
            DBActivityPriceChanged lastPriceChange = ObjectConverter.ConvertToDBType(MongoDBInteractor.GetLastPriceChangeOfApp(a.id));
            DBActivityPriceChanged priceChange = new DBActivityPriceChanged();
            priceChange.parentApplication.id = a.id;
            priceChange.parentApplication.hmd = a.hmd;
            priceChange.parentApplication.canonicalName = a.canonicalName;
            priceChange.parentApplication.displayName = a.displayName;
            priceChange.newPriceOffsetNumerical = a.priceOffsetNumerical;
            priceChange.newPriceFormatted = a.priceFormatted;
            if (lastPriceChange != null)
            {
                if (lastPriceChange.newPriceOffset != priceChange.newPriceOffset)
                {
                    priceChange.oldPriceFormatted = lastPriceChange.newPriceFormatted;
                    priceChange.oldPriceOffset = lastPriceChange.newPriceOffset;
                    priceChange.__lastEntry = lastPriceChange.__id;
                    DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
                }
            }
            else
            {
                DiscordWebhookSender.SendActivity(ScrapingNodeMongoDBManager.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument(), scrapingContribution.scrapingNode), ref scrapingContribution);
            }
            
            ScrapingNodeMongoDBManager.AddApplication(a, ref scrapingContribution);
            r.processedCount++;
        }

        foreach (DBAppImage img in taskResult.scraped.imgs)
        {
            ScrapingNodeMongoDBManager.AddImage(img, ref scrapingContribution);
        }

        r.processed = true;
        scrapingContribution.lastContribution = DateTime.UtcNow;
        ScrapingNodeMongoDBManager.UpdateScrapingNodeStats(scrapingContribution);
    }

    /// <summary>
    /// Process heartbeats from scraping nodes
    /// </summary>
    /// <param name="heartBeat"></param>
    /// <param name="scrapingNodeAuthenticationResult"></param>
    public static void ProcessHeartBeat(ScrapingNodeHeartBeat heartBeat, ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {
        ScrapingNodeStats stats = ScrapingNodeMongoDBManager.GetScrapingNodeStats(scrapingNodeAuthenticationResult.scrapingNode);
        stats.snapshot = heartBeat.snapshot;
        // If online, add time since last heartbeat to runtime
        DateTime now = DateTime.UtcNow;
        if (stats.online)
        {
            stats.runtime += now - stats.lastHeartBeat;
            stats.totalRuntime += now - stats.lastHeartBeat;
        }
        stats.lastHeartBeat = now;
        
        ScrapingNodeMongoDBManager.UpdateScrapingNodeStats(stats);
    }

    public static void OnNodeStarted(ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {
        // If node was responsible for something that locks other nodes from doing it, unlock it
        isAppAddingRunning.Unlock(scrapingNodeAuthenticationResult.scrapingNode);
        ScrapingNodeStats s =
            ScrapingNodeMongoDBManager.GetScrapingNodeStats(scrapingNodeAuthenticationResult.scrapingNode);
        // reset runtime as node has just restarted
        s.runtime = TimeSpan.Zero;
        s.lastRestart = DateTime.UtcNow;
        s.lastHeartBeat = DateTime.UtcNow;
        ScrapingNodeMongoDBManager.UpdateScrapingNodeStats(s);
    }
}