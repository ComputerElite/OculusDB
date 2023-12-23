using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBReleaseChannel : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ReleaseChannel;
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [TrackChanges]
    [BsonElement("n")]
    public string name { get; set; } = "";
}