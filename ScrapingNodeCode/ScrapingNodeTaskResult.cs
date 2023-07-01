using MongoDB.Bson.Serialization.Attributes;
using OculusDB.Database;
using OculusDB.ScrapingMaster;

namespace OculusDB.ScrapingNodeCode;


[BsonIgnoreExtraElements]
public class ScrapingNodeTaskResult
{
    public ScrapingNodeTaskResultType scrapingNodeTaskResultType { get; set; } = ScrapingNodeTaskResultType.Unknown;
    public List<AppToScrape> appsToScrape { get; set; } = new List<AppToScrape>();
    public ConnectedListWithImages scraped { get; set; } = new ();
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