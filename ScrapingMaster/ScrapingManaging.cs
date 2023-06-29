using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusDB.ScrapingNodeCode;
using OculusDB.Users;

namespace OculusDB.ScrapingMaster;

public class ScrapingManaging
{
    public static TimeDependantBool isAppAddingRunning { get; set; } = new TimeDependantBool();
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

    public static ScrapingProcessedResult ProcessScrapingResults()
    {
        ScrapingProcessedResult r = new ScrapingProcessedResult();
        return r;
    }

    public static void ProcessTaskResult(ScrapingNodeTaskResult taskResult, ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {
        Logger.Log("Results of Scraping node " + scrapingNodeAuthenticationResult.scrapingNode + " received. Processing now...");
        Logger.Log("Result type: " +
                   Enum.GetName(typeof(ScrapingNodeTaskResultType), taskResult.scrapingNodeTaskResultType));
        // Process results of scraping:
        //   - When apps for scraping have been sent, add them to the DB for scraping
        //   - On error while requesting apps to scrape, make other scraping nodes able to request apps to scrape
        //   - When scraping is done, compute the activity entries and write both to the DB (Each scraped app should)
        switch (taskResult.scrapingNodeTaskResultType)
        {
            case ScrapingNodeTaskResultType.ErrorWhileRequestingAppsToScrape:
                if (!isAppAddingRunning.IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
                {
                    Logger.Log("Node is not responsible for adding apps to scrape. Ignoring error.");
                    return;
                }
                Logger.Log("Error while requesting apps to scrape. Making other scraping nodes able to request apps to scrape.");
                isAppAddingRunning.Set(false, TimeSpan.FromMinutes(10), "");
                break;
            case ScrapingNodeTaskResultType.FoundAppsToScrape:
                if (!isAppAddingRunning.IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
                {
                    Logger.Log("Node is not responsible for adding apps to scrape. Ignoring.");
                    return;
                }
                Logger.Log("Found apps to scrape. Adding them to the DB.");
                ScrapingNodeMongoDBManager.AddAppsToScrape(taskResult.appsToScrape, scrapingNodeAuthenticationResult.scrapingNode);
                break;
            case ScrapingNodeTaskResultType.AppsScraped:
                ProcessScrapedResults(taskResult, scrapingNodeAuthenticationResult);
                break;
        }
    }

    private static void ProcessScrapedResults(ScrapingNodeTaskResult taskResult, ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {
        Logger.Log("Processing " + taskResult.scraped.applications.Count + " applications, " + taskResult.scraped.dlcs.Count + " dlcs, " + taskResult.scraped.dlcPacks.Count + " dlc packs and " + taskResult.scraped.versions.Count + " version from scraping node " + scrapingNodeAuthenticationResult.scrapingNode);
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
                e.releaseDate = TimeConverter.UnixTimeStampToDateTime((long)a.release_date);
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
        }
    }
    
    public string FormatPrice(long offsetAmount, string currency)
    {
        string symbol = "";
        if (currency == "USD") symbol = "$";
        if (currency == "EUR") symbol = "€";
        string price = symbol + String.Format("{0:0.00}", offsetAmount / 100.0);
            
        return price;
    }
}