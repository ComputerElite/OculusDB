using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;

[BsonIgnoreExtraElements]
public class ScrapingNode
{
    public string scrapingNodeId { get; set; } = "";
    [JsonIgnore] // Don't send the token to anything via JsonSerializer. Only the DB and MasterScrapingManager should know the token.
    public string scrapingNodeToken { get; set; } = "";
    public string scrapingNodeName { get; set; } = "";
    public string scrapingNodeVersion { get; set; } = "0.0";
    public DateTime expires { get; set; } = DateTime.MinValue;

    public override string ToString()
    {
        return scrapingNodeName + "(" + scrapingNodeId + ")";
    }
}