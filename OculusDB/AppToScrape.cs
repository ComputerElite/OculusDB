using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using OculusGraphQLApiLib;

namespace OculusDB;

public class AppToScrape
{

    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public string _id { get; set; }
        
    public string appId { get; set; } = "";
    public bool priority { get; set; } = false;
    public string canonicalName { get; set; } = "";
    public DateTime addedTime { get; set; } = DateTime.UtcNow;
    public DateTime sentToScrapeTime { get; set; } = DateTime.MinValue;
    public AppScrapePriority scrapePriority { get; set; } = AppScrapePriority.Low;
    public string currency { get; set; } = "";

    public string responsibleScrapingNodeId { get; set; } = "";
}