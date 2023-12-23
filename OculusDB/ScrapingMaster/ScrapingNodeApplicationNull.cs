using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.Database;

namespace OculusDB.ScrapingMaster;

public class ScrapingNodeApplicationNull
{
    public string __OculusDBType { get; set; } = DBDataTypes.ApplicationNull;
    /// <summary>
    /// Only 100 most recent reports
    /// </summary>
    public List<string> reportedBy { get; set; } = new List<string>();
    [BsonIgnore]
    public ScrapingNodeIdentification identification { get; set; } = new ScrapingNodeIdentification();
    public string applicationId { get; set; } = "";
    public long count { get; set; } = 0;
}