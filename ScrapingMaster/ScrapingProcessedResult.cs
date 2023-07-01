using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingProcessedResult
{
    public bool processed { get; set; } = true;
    public int processedCount { get; set; } = 0;
    public int failedCount { get; set; } = 0;
    public string msg { get; set; } = "";
}