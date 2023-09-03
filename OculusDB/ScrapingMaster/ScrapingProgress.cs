using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingProgress
{
    public int totalTasks { get; set; } = 0;
    public int completedTasks { get; set; } = 0;
    public int failedTasks { get; set; } = 0;
    public Dictionary<string, long> queuedDocuments { get; set; } = new();
}