namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeTaskResult
{
    public ScrapingNodeTaskResultType scrapingNodeTaskResultType { get; set; } = ScrapingNodeTaskResultType.Unknown;
    public List<AppToScrape> appsToScrape { get; set; } = new List<AppToScrape>();
    public bool altered = false;
}

public enum ScrapingNodeTaskResultType
{
    FoundAppsToScrape,
    AppsScraped,
    Unknown,
    ErrorWhileRequestingAppsToScrape
}