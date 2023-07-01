using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;

[BsonIgnoreExtraElements]
public class ScrapingNodeSnapshot
{
    public long doneTasks { get; set; } = 0;
    public long totalTasks { get; set; } = 0;
    public ScrapingNodeStatus scrapingStatus { get; set; } = ScrapingNodeStatus.Idling;
    public Dictionary<string, long> queuedDocuments { get; set; } = new Dictionary<string, long>();
}