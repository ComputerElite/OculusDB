using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBIAPItemId : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IapItemId;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
}