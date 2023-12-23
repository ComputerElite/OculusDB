using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBAchievementTranslation : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AchievementTranslation;
    [TrackChanges]
    [BsonElement("l")]
    public string locale { get; set; } = "";
    [TrackChanges]
    [BsonElement("d")]
    public string description { get; set; } = "";
    [TrackChanges]
    [BsonElement("t")]
    public string title { get; set; } = "";
    [TrackChanges]
    [BsonElement("u")]
    public string unlockedDescription { get; set; } = "";
}