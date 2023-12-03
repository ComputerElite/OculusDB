using System.Diagnostics;
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
                    r.msg = "Processed " + taskResult.scraped.applications.Count + " applications, " + taskResult.scraped.dlcs.Count + " dlcs, " + taskResult.scraped.dlcPacks.Count + " dlc packs, " + taskResult.scraped.versions.Count + " version and " + taskResult.scraped.imgs.Count + " images from scraping node " + scrapingNodeAuthenticationResult.scrapingNode + ". Thanks for your contribution.";
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
        Logger.Log("# " + taskId + "  Processing " + taskResult.scraped.applications.Count + " applications, " + taskResult.scraped.dlcs.Count + " dlcs, " + taskResult.scraped.dlcPacks.Count + " dlc packs, " + taskResult.scraped.versions.Count + " version and " + taskResult.scraped.imgs.Count + " images from scraping node " + scrapingNodeAuthenticationResult.scrapingNode);
        ScrapingContribution scrapingContribution = new ScrapingContribution();
        scrapingContribution.scrapingNode = scrapingNodeAuthenticationResult.scrapingNode;
        // Process Versions
        Dictionary<string, List<DBVersion>> versionLookup = new Dictionary<string, List<DBVersion>>();
        ScrapingProcessingStats stats = new ScrapingProcessingStats();
        stats.scrapingNode = scrapingNodeAuthenticationResult.scrapingNode;
        stats.processStartTime = DateTime.Now;
        stats.nodesProcessingAtStart = processingRn.Sum(x => x.Value.processingCount);
        Stopwatch sw = Stopwatch.StartNew();
        
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.versions.Count + " versions");
        foreach (DBVersion v in taskResult.scraped.versions)
        {
            DBApplication parentApplication =
                taskResult.scraped.applications.FirstOrDefault(x => x.id == v.parentApplication.id);
            if (parentApplication == null)
            {
                Logger.Log("Skipping " + v + " because the parent application isn't in the scraping results");
                continue;
            }

            BsonDocument lastActivity = null;
            if (!versionLookup.ContainsKey(v.parentApplication.id))
            {
                // Add versions to VersionLookup
                versionLookup.Add(v.parentApplication.id, MongoDBInteractor.GetVersions(v.parentApplication.id, false));
            }

            DBVersion oldEntry = versionLookup[v.parentApplication.id].FirstOrDefault(x => x.id == v.id);
            
            
            // Update contributions
            ScrapingNodeMongoDBManager.AddVersion(v, ref scrapingContribution);
            r.processedCount++;
        }
        stats.versionProcessTime = sw.Elapsed;
        stats.versionsProcessed = taskResult.scraped.versions.Count;
        sw.Restart();
        // Process DLCs
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.dlcs.Count + " dlcs");
        foreach (DBIAPItem d in taskResult.scraped.dlcs)
        {
            DBApplication parentApplication =
                null;
            if (parentApplication == null)
            {
                Logger.Log("Skipping " + d + " because the parent application isn't in the scraping results");
                continue;
            }
            
            r.processedCount++;
        }
        stats.dlcProcessTime = sw.Elapsed;
        stats.dlcsProcessed = taskResult.scraped.dlcs.Count;
        sw.Restart();
        
        // Process DLC Packs
        Dictionary<string, List<DBIAPItem>> dlcsInDB = new ();
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.dlcPacks.Count + " dlc packs");
        foreach (DBIAPItemPack d in taskResult.scraped.dlcPacks)
        {
            DBApplication parentApplication =
                taskResult.scraped.applications.FirstOrDefault(x => x.id == d.parentApplication.id);
            if (parentApplication == null)
            {
                Logger.Log("Skipping " + d + " because the parent application isn't in the scraping results");
                continue;
            }
            if (!dlcsInDB.ContainsKey(parentApplication.id))
            {
                dlcsInDB.Add(parentApplication.id, ScrapingNodeMongoDBManager.GetDLCs(parentApplication.id));
            }
            
            r.processedCount++;
        }
        stats.dlcPackProcessTime = sw.Elapsed;
        stats.dlcPacksProcessed = taskResult.scraped.dlcPacks.Count;
        sw.Restart();
        
        // Process Applications
        Logger.Log("# " + taskId + " processing " + taskResult.scraped.applications.Count + " applications");
        foreach (DBApplication a in taskResult.scraped.applications)
        {
            Logger.Log("# " + taskId + " processing " + a.id);
            if (a == null)
            {
                ReportErrorWithDiscordMessage("Application is null. Scraping node is " + scrapingNodeAuthenticationResult.scrapingNode.scrapingNodeId, "Application is null");
                continue;
            }
            // New Application activity
            bool isNew = false;
            
            ScrapingNodeMongoDBManager.AddApplication(a, ref scrapingContribution);
            
            r.processedCount++;
        }
        stats.appProcessTime = sw.Elapsed;
        stats.appsProcessed = taskResult.scraped.applications.Count;
        sw.Stop();

        Logger.Log("# " + taskId + " processing " + taskResult.scraped.imgs.Count + " images");
        foreach (DBAppImage img in taskResult.scraped.imgs)
        {
            ScrapingNodeMongoDBManager.AddImage(img, ref scrapingContribution);
        }

        r.processed = true;
        Logger.Log("# " + taskId + " processing done. Incrementing stats");
        scrapingContribution.lastContribution = DateTime.UtcNow;
        stats.processEndTime = DateTime.UtcNow;
        stats.nodesProcessingAtEnd = processingRn.Sum(x => x.Value.processingCount);
        ScrapingNodeMongoDBManager.AddScrapingProcessingStat(stats);
        scrapingContribution.taskResultsProcessed = 1;
        ScrapingNodeMongoDBManager.IncScrapingNodeContribution(scrapingContribution);
        Logger.Log("# " + taskId + " flushing");
        ScrapingNodeMongoDBManager.Flush();
        Logger.Log("# " + taskId + " flushed");
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
