using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBAchievement : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Achievement;
    [ObjectScrapingNodeFieldPresent]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    public string id { get; set; } = "";
    
    [OculusField("api_name")]
    public string apiName { get; set; } = "";
    [OculusFieldAlternate("achievement_type_enum")]
    public AchievementType achievementType { get; set; } = AchievementType.UNKNOWN;
    [BsonIgnore]
    public string achievementTypeFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(achievementType.ToString());
        }
    }
    [OculusFieldAlternate("achievement_write_policy_enum")]
    public AchievementWritePolicy achievementWritePolicy { get; set; } = AchievementWritePolicy.UNKNOWN;
    [BsonIgnore]
    public string achievementWritePolicyFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(achievementWritePolicy.ToString());
        }
    }
    [OculusField("is_draft")]
    public bool isDraft { get; set; } = false;
    [OculusField("is_secret")]
    public bool isSecret { get; set; } = false;
    [OculusField("is_archived")]
    public bool isArchived { get; set; } = false;
    [ListScrapingNodeFieldPresent]
    public List<DBAchievementTranslation> translations { get; set; } = new List<DBAchievementTranslation>();
    [BsonIgnore]
    public string? title
    {
        get
        {
            return translations.FirstOrDefault()?.title ?? null;
        }
    }
    [BsonIgnore]
    public string? description
    {
        get
        {
            return translations.FirstOrDefault()?.description ?? null;
        }
    }
    [BsonIgnore]
    public string? unlockedDescription
    {
        get
        {
            return translations.FirstOrDefault()?.unlockedDescription ?? null;
        }
    }
}