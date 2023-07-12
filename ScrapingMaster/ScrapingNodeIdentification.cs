using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingNodeIdentification
{
    public string scrapingNodeToken { get; set; } = "";
    public string scrapingNodeVersion { get; set; } = "1.0.0";
    public string currency { get; set; } = "";
    public int tokenCount { get; set; } = 0;
}