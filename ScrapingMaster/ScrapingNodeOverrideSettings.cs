using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;

[BsonIgnoreExtraElements]
public class ScrapingNodeOverrideSettings
{
    public ScrapingNode scrapingNode { get; set; } = new();
    public string overrideCurrency { get; set; } = "";
}