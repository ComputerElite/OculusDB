namespace OculusDB.ScrapingMaster;

public class ScrapingTask
{
    public ScrapingTaskType scrapingTask { get; set; } = ScrapingTaskType.GetAllAppsToScrape;
    public AppToScrape appToScrape { get; set; } = new AppToScrape();
}

public enum ScrapingTaskType
{
    GetAllAppsToScrape,
    Wait1Minute,
    ScrapeApp
}