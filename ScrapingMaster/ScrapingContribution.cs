using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingContribution
{
    public DateTime lastContribution { get; set; } = DateTime.MinValue;
    public Dictionary<string, long> contributionPerOculusDBType { get; set; } = new();
    public long appsQueuedForScraping { get; set; } = 0;
    [JsonIgnore]
    public ScrapingNode scrapingNode { get; set; } = new ScrapingNode();

    public void AddContribution(string type, long toAdd)
    {
        if(!contributionPerOculusDBType.ContainsKey(type)) contributionPerOculusDBType.Add(type, 0);
        contributionPerOculusDBType[type] += toAdd;
    }
}