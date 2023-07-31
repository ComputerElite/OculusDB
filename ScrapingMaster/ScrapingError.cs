using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace OculusDB.ScrapingMaster;

public class ScrapingError
{
    
    [BsonIgnore]
    public string __id { get
    {
        return _id; 
    } }
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonIgnore]
    public string _id { get; set; }
    public string scrapingNodeId { get; set; } = "";
    public AppToScrape appToScrape { get; set; } = new AppToScrape();
    public string errorMessage { get; set; } = "";
}