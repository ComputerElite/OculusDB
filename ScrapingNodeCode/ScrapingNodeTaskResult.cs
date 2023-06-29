using OculusDB.Database;
using OculusDB.ScrapingMaster;

namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeTaskResult
{
    public ScrapingNodeTaskResultType scrapingNodeTaskResultType { get; set; } = ScrapingNodeTaskResultType.Unknown;
    public List<AppToScrape> appsToScrape { get; set; } = new List<AppToScrape>();
    public ConnectedList scraped { get; set; } = new ConnectedList();
    public ScrapingNodeIdentification identification { get; set; } = new ScrapingNodeIdentification();
    public bool altered = false;
}

public enum ScrapingNodeTaskResultType
{
    FoundAppsToScrape,
    AppsScraped,
    Unknown,
    ErrorWhileRequestingAppsToScrape
}