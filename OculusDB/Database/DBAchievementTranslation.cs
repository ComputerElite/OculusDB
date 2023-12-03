using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.Database;

public class DBAchievementTranslation : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AchievementTranslation;
    public string locale { get; set; } = "";
    public string description { get; set; } = "";
    public string title { get; set; } = "";
    public string unlockedDescription { get; set; } = "";
}