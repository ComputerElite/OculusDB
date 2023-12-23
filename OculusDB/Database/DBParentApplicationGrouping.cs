using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBParentApplicationGrouping : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ParentApplicationGrouping;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [BsonElement("a")]
    public List<DBParentApplication> applications { get; set; } = new List<DBParentApplication>();
}