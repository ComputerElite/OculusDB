using System.Text.Json.Serialization;

namespace OculusDB.ScrapingMaster;

public class ScrapingNode
{
    public string scrapingNodeId { get; set; } = "";
    [JsonIgnore] // Don't send the token to anything via JsonSerializer. Only the DB and MasterScrapingManager should know the token.
    public string scrapingNodeToken { get; set; } = "";
    public string scrapingNodeName { get; set; } = "";
    public DateTime expires { get; set; } = DateTime.MinValue;
}