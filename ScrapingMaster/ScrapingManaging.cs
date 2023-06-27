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
}