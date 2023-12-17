using System.Diagnostics;
using System.Net;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using OculusDB.Database;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusDB.ScrapingNodeCode;
using OculusDB.Users;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Memory;

namespace OculusDB.ScrapingMaster;

public class ScrapingManaging
{
    public static ulong processTaskId = 0;
    public static Dictionary<string, TimeDependantBool> isAppAddingRunning { get; set; } = new ();
    public static Dictionary<string, ScrapingNodeTaskResultProcessing> processingRn = new ();
    public static List<ScrapingTask> GetTasks(ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {            
        string currency = scrapingNodeAuthenticationResult.scrapingNode.currency;
        if (ScrapingNodeMongoDBManager.GetNonPriorityAppsToScrapeCount(currency) <= 20)
        {
            // Apps to scrape should be added
            if(!isAppAddingRunning.ContainsKey(currency))
            {
                isAppAddingRunning.Add(scrapingNodeAuthenticationResult.scrapingNode.currency, new());
            }
            if (!isAppAddingRunning[currency])
            {
                // Request node to get all apps on Oculus for scraping. Results should be returned within 10 minutes.
                isAppAddingRunning[currency].Set(true, TimeSpan.FromMinutes(10), scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId);
                return new List<ScrapingTask>
                {
                    new()
                    {
                        scrapingTask = ScrapingTaskType.GetAllAppsToScrape
                    }
                };
            }
            if (isAppAddingRunning[currency].IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
            {
                // Why tf are you requesting tasks. You should be getting all apps to scrape rn you idiot
                return new List<ScrapingTask>
                {
                    new()
                    {
                        scrapingTask = ScrapingTaskType.WaitForResults
                    }
                };
            }
        }

        if (processingRn.TryGetValue(scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId, out ScrapingNodeTaskResultProcessing processing))
        {
            if (processing.processingCount >= 5) // Allow up to 5 submissions per node to be processed at the same time
            {
                return new List<ScrapingTask>
                {
                    new()
                    {
                        scrapingTask = ScrapingTaskType.WaitForResults
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

        string currency = scrapingNodeAuthenticationResult.scrapingNode.currency;
        if (!isAppAddingRunning.ContainsKey(currency))
        {
            isAppAddingRunning.Add(scrapingNodeAuthenticationResult.scrapingNode.currency, new());
        }
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
                if (!isAppAddingRunning[currency].IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
                {
                    Logger.Log("Node is not responsible for adding apps to scrape. Ignoring error.");
                    r.processed = false;
                    r.msg = "You are not responsible for adding apps to scrape. Your submission has been ignored.";
                    r.failedCount = 1;
                    processingRn[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId].Done();
                    return r;
                }
                Logger.Log("Error while requesting apps to scrape. Making other scraping nodes able to request apps to scrape.");
                isAppAddingRunning[currency].Set(false, TimeSpan.FromMinutes(10), "");
                break;
            case ScrapingNodeTaskResultType.FoundAppsToScrape:
                if (!isAppAddingRunning[currency].IsThisResponsible(scrapingNodeAuthenticationResult.scrapingNode))
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
                try
                {
                    ProcessScrapedResults(taskResult, scrapingNodeAuthenticationResult, ref r);
                    r.msg = "Processed " + taskResult.scraped.applications.Count + " applications, " 
                            + taskResult.scraped.iapItems.Count + " iaps, " + taskResult.scraped.iapItemPacks.Count 
                            + " iap packs, " + taskResult.scraped.versions.Count + " version, " + taskResult.scraped.achievements
                            + " achievements, " + taskResult.scraped.offers + " offers and " + taskResult.scraped.imgs.Count 
                            + " images from scraping node " + scrapingNodeAuthenticationResult.scrapingNode + ". Thanks for your contribution.";
                }
                catch (Exception e)
                {
                    Logger.Log("Error while processing scraped results of node " + scrapingNodeAuthenticationResult.scrapingNode + ": " + e, LoggingType.Warning);
                    ReportErrorWithDiscordMessage(e.ToString());
                }
                break;
        }
        processingRn[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId].Done();
        return r;
    }

    public static void ReportErrorWithDiscordMessage(string errorMsg, string discordDescription = "")
    {
        ScrapingError error = ScrapingNodeMongoDBManager.AddErrorReport(new ScrapingError
        {
            errorMessage = errorMsg,
            scrapingNodeId = "MASTER-SERVER"
        }, new ScrapingNodeAuthenticationResult
        {
            scrapingNode = new ScrapingNode
            {
                scrapingNodeId = "MASTER-SERVER"
            }
        });
        ScrapingMasterServer.SendMasterWebhookMessage("Processing error", discordDescription + "\n" + OculusDBEnvironment.config.scrapingMasterUrl + "api/v1/scrapingerror/" + error.__id, 0xFF8800);
    }

    private static void ProcessScrapedResults(ScrapingNodeTaskResult taskResult, ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult, ref ScrapingProcessedResult r)
    {
        ulong taskId = processTaskId;
        processTaskId++;
        string scrapingNodeId = scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId;
        ScrapingContribution scrapingContribution = new ScrapingContribution();
        scrapingContribution.scrapingNode = scrapingNodeAuthenticationResult.scrapingNode;
        // Process Versions
        Dictionary<string, List<DBVersion>> versionLookup = new Dictionary<string, List<DBVersion>>();
        
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.versions.Count + " versions");
        foreach (DBVersion v in taskResult.scraped.versions)
        {
            string? parentApp = v.parentApplication?.id ?? null;

            if (parentApp != null && !versionLookup.ContainsKey(parentApp))
            {
                // Add versions to VersionLookup
                versionLookup.Add(parentApp, DBVersion.GetVersionsOfAppId(parentApp));
            }
            if (parentApp == "")
            {
                // If no grouping then get it individually from the db
                DBVersion? found = DBVersion.ById(v.id);
                if(found != null) versionLookup[""].Add(found);
            }

            DBVersion? oldEntry = v.GetEntryForDiffGeneration(versionLookup[v.parentApplication.id]);
            ScrapingNodeMongoDBManager.AddDiff(DiffMaker.GetDifference(oldEntry, v, scrapingNodeId), ref scrapingContribution);
            
            // Update contributions
            ScrapingNodeMongoDBManager.AddVersion(v, ref scrapingContribution);
            r.processedCount++;
        }
        versionLookup.Clear();
        
        // Process DLCs
        Dictionary<string, List<DBIAPItem>> iapItemLookup = new Dictionary<string, List<DBIAPItem>>();
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.iapItems.Count + " iaps");
        foreach (DBIAPItem d in taskResult.scraped.iapItems)
        {
            string? parentGrouping = d.grouping?.id ?? null;

            if (parentGrouping != null && !iapItemLookup.ContainsKey(parentGrouping))
            {
                // Add versions to VersionLookup
                iapItemLookup.Add(parentGrouping, DBIAPItem.GetAllForApplicationGrouping(parentGrouping));
            }
            if (parentGrouping == "")
            {
                // If no grouping then get it individually from the db
                DBIAPItem? found = DBIAPItem.ById(d.id);
                if(found != null) iapItemLookup[""].Add(found);
            }

            DBIAPItem? oldEntry = d.GetEntryForDiffGeneration(iapItemLookup[parentGrouping ?? ""]);
            ScrapingNodeMongoDBManager.AddDiff(DiffMaker.GetDifference(oldEntry, d, scrapingNodeId), ref scrapingContribution);
            
            // Update contributions
            ScrapingNodeMongoDBManager.AddIapItem(d, ref scrapingContribution);
            r.processedCount++;
        }
        iapItemLookup.Clear();
        
        
        // Process IapPacks
        Dictionary<string, List<DBIAPItemPack>> iapItemPackLookup = new Dictionary<string, List<DBIAPItemPack>>();
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.iapItemPacks.Count + " iap packs");
        foreach (DBIAPItemPack d in taskResult.scraped.iapItemPacks)
        {
            string? parentGrouping = d.grouping?.id ?? null;
            if (parentGrouping != null && !iapItemPackLookup.ContainsKey(parentGrouping))
            {
                // Cache everything for grouping
                iapItemPackLookup.Add(parentGrouping, DBIAPItemPack.GetAllForApplicationGrouping(parentGrouping));
            }
            if (parentGrouping == "")
            {
                // If no grouping then get it individually from the db
                DBIAPItemPack? found = DBIAPItemPack.ById(d.id);
                if(found != null) iapItemPackLookup[""].Add(found);
            }

            DBIAPItemPack? oldEntry = d.GetEntryForDiffGeneration(iapItemPackLookup[parentGrouping ?? ""]);
            ScrapingNodeMongoDBManager.AddDiff(DiffMaker.GetDifference(oldEntry, d, scrapingNodeId), ref scrapingContribution);
            
            // Update contributions
            ScrapingNodeMongoDBManager.AddIapItemPack(d, ref scrapingContribution);
            r.processedCount++;
        }
        iapItemPackLookup.Clear();
        
        // Process offers
        Dictionary<string, List<DBOffer>> offerLookup = new Dictionary<string, List<DBOffer>>();
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.offers.Count + " offers");
        foreach (DBOffer d in taskResult.scraped.offers)
        {
            string? parentApp = d.parentApplication?.id ?? null;
            if (parentApp != null && !offerLookup.ContainsKey(parentApp))
            {
                // Cache everything for grouping
                offerLookup.Add(parentApp, DBOffer.GetAllForApplication(parentApp));
            }
            if (parentApp == "")
            {
                // If no grouping then get it individually from the db
                List<DBOffer> found = DBOffer.ById(d.id);
                offerLookup[""].AddRange(found);
            }
            d.presentOn = GetOfferPresentOn(d, ref taskResult);
            DBOffer? oldEntry = d.GetEntryForDiffGeneration(offerLookup[parentApp ?? ""]);
            ScrapingNodeMongoDBManager.AddDiff(DiffMaker.GetDifference(oldEntry, d, scrapingNodeId), ref scrapingContribution);
            
            // Update contributions
            ScrapingNodeMongoDBManager.AddOffer(d, ref scrapingContribution);
            r.processedCount++;
        }
        offerLookup.Clear();
        
        
        
        // Process Achievements
        Dictionary<string, List<DBAchievement>> achievementLoopup = new Dictionary<string, List<DBAchievement>>();
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.achievements.Count + " achievements");
        foreach (DBAchievement d in taskResult.scraped.achievements)
        {
            string? parentGrouping = d.grouping?.id ?? null;
            if (parentGrouping != null && !achievementLoopup.ContainsKey(parentGrouping))
            {
                // Cache everything for grouping
                achievementLoopup.Add(parentGrouping, DBAchievement.GetAllForApplicationGrouping(parentGrouping));
            }
            if (parentGrouping == "")
            {
                // If no grouping then get it individually from the db
                DBAchievement? found = DBAchievement.ById(d.id);
                if(found != null) achievementLoopup[""].Add(found);
            }

            DBAchievement? oldEntry = d.GetEntryForDiffGeneration(achievementLoopup[parentGrouping ?? ""]);
            ScrapingNodeMongoDBManager.AddDiff(DiffMaker.GetDifference(oldEntry, d, scrapingNodeId), ref scrapingContribution);
            
            // Update contributions
            ScrapingNodeMongoDBManager.AddAchievement(d, ref scrapingContribution);
            r.processedCount++;
        }
        achievementLoopup.Clear();
        
        
        
        // Process Applications
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.achievements.Count + " achievements");
        foreach (DBApplication d in taskResult.scraped.applications)
        {
            DBApplication? oldEntry = d.GetEntryForDiffGenerationFromDB();
            ScrapingNodeMongoDBManager.AddDiff(DiffMaker.GetDifference(oldEntry, d, scrapingNodeId), ref scrapingContribution);
            
            // Update contributions
            ScrapingNodeMongoDBManager.AddApplication(d, ref scrapingContribution);
            ScrapingNodeMongoDBManager.RemoveAppFromAppsScraping(d);
            r.processedCount++;
        }
        

        Logger.Log("# " + taskId + " processing " + taskResult.scraped.imgs.Count + " images");
        foreach (DBAppImage img in taskResult.scraped.imgs)
        {
            ScrapingNodeMongoDBManager.AddImage(img, ref scrapingContribution);
        }

        r.processed = true;
        Logger.Log("# " + taskId + " processing done. Incrementing stats");
        scrapingContribution.lastContribution = DateTime.UtcNow;
        scrapingContribution.taskResultsProcessed = 1;
        ScrapingNodeMongoDBManager.IncScrapingNodeContribution(scrapingContribution);
        Logger.Log("# " + taskId + " flushing");
        ScrapingNodeMongoDBManager.Flush();
        Logger.Log("# " + taskId + " flushed");
    }

    private static List<string> GetOfferPresentOn(DBOffer dbOffer, ref ScrapingNodeTaskResult taskResult)
    {
        string offerId = dbOffer.id;
        List<string> presentOn = new List<string>();
        
        // Find all instances of offer in DB and TaskResult
        presentOn.AddRange(taskResult.scraped.iapItemPacks.Where(x => x.offerId == offerId).ToList().Select(x => x.id));
        presentOn.AddRange(taskResult.scraped.iapItems.Where(x => x.offerId == offerId).ToList().Select(x => x.id));
        presentOn.AddRange(taskResult.scraped.applications.Where(x => x.offerId == offerId).ToList().Select(x => x.id));
        presentOn.AddRange(OculusDBDatabase.iapItemCollection.Find(x => x.offerId == offerId).ToList().Select(x => x.id));
        presentOn.AddRange(OculusDBDatabase.iapItemPackCollection.Find(x => x.offerId == offerId).ToList().Select(x => x.id));
        presentOn.AddRange(OculusDBDatabase.applicationCollection.Find(x => x.offerId == offerId).ToList().Select(x => x.id));
        
        // Remove duplicates
        return presentOn.Distinct().ToList();
    }

    public static Dictionary<string, DateTime> OAuthExceptionReportTimes = new ();

    /// <summary>
    /// Process heartbeats from scraping nodes
    /// </summary>
    /// <param name="heartBeat"></param>
    /// <param name="scrapingNodeAuthenticationResult"></param>
    public static ScrapingNodeHeartBeatProcessed ProcessHeartBeat(ScrapingNodeHeartBeat heartBeat, ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult)
    {
        ScrapingNodeStats stats = ScrapingNodeMongoDBManager.GetScrapingNodeStats(scrapingNodeAuthenticationResult.scrapingNode);
        if (stats == null)
        {
            return new ScrapingNodeHeartBeatProcessed
            {
                processed = false,
                msg = "Couldn't find status entry at this time. Next heartbeat should get processed."
            };
        }
        
        // If online, add time since last heartbeat to runtime only if not priority scrape to prevent discord spam
        
        stats.snapshot = heartBeat.snapshot;
        DateTime now = DateTime.UtcNow;
        if (stats.online)
        {
            stats.runtime += now - stats.lastHeartBeat;
            stats.totalRuntime += now - stats.lastHeartBeat;
        }
        stats.lastHeartBeat = now;

        if(!OAuthExceptionReportTimes.ContainsKey(scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId)) OAuthExceptionReportTimes.Add(scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId, DateTime.MinValue);
        if (stats.status == ScrapingNodeStatus.OAuthException && (DateTime.Now - OAuthExceptionReportTimes[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId]).TotalDays >= 1)
        {
            OAuthExceptionReportTimes[scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId] = DateTime.Now;
            // Send message on Discord
            ScrapingMasterServer.SendMasterWebhookMessage("OAuth Exception", "OAuth Exception on scraping node " + scrapingNodeAuthenticationResult.scrapingNode + ". This node should update its Token!", 0xFF0000);
        }
        if(!heartBeat.identification.isPriorityScrape) ScrapingNodeMongoDBManager.UpdateScrapingNodeStats(stats);
        return new ScrapingNodeHeartBeatProcessed();
    }

    public static void OnNodeStarted(ScrapingNodeAuthenticationResult scrapingNodeAuthenticationResult, ScrapingNodeIdentification scrapingNodeIdentification)
    {
        // If node was responsible for something that locks other nodes from doing it, unlock it
        string currency = scrapingNodeAuthenticationResult.scrapingNode.currency;
        if (isAppAddingRunning.ContainsKey(currency))
        {
            isAppAddingRunning[currency].Unlock(scrapingNodeAuthenticationResult.scrapingNode);
        }
        ScrapingNodeStats s =
            ScrapingNodeMongoDBManager.GetScrapingNodeStats(scrapingNodeAuthenticationResult.scrapingNode);
        if (scrapingNodeIdentification.isPriorityScrape) return;
        if (s == null)
        {
            return;
        }
        // reset runtime as node has just restarted
        s.runtime = TimeSpan.Zero;
        s.lastRestart = DateTime.UtcNow;
        s.lastHeartBeat = DateTime.UtcNow;
        ScrapingNodeMongoDBManager.UpdateScrapingNodeStats(s);
    }

    public static Dictionary<string, TimeDependantBool> GetAppAdding()
    {
        return isAppAddingRunning;
    }

    public static AppCount GetAppCount(string? currency)
    {
        return new AppCount
        {
            count = ScrapingNodeMongoDBManager.GetNonPriorityAppsToScrapeCount(currency ?? ""),
            currency = currency ?? ""
        };
    }
}
