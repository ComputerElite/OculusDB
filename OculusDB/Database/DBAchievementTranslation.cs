using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBAchievementTranslation : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AchievementTranslation;
    [TrackChanges]
    public string locale { get; set; } = "";
    [TrackChanges]
    public string description { get; set; } = "";
    [TrackChanges]
    public string title { get; set; } = "";
    [TrackChanges]
    public string unlockedDescription { get; set; } = "";
}