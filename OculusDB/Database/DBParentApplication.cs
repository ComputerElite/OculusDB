using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBParentApplication : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ParentApplication;
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [TrackChanges]
    [BsonElement("n")]
    public string displayName { get; set; } = "";
}