using System.Linq.Expressions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBAchievement : DBBase, IDBObjectOperations<DBAchievement>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Achievement;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    
    [OculusField("api_name")]
    [TrackChanges]
    public string apiName { get; set; } = "";
    [OculusFieldAlternate("achievement_type_enum")]
    [TrackChanges]
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
    [TrackChanges]
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
    [TrackChanges]
    public bool isDraft { get; set; } = false;
    [OculusField("is_secret")]
    [TrackChanges]
    public bool isSecret { get; set; } = false;
    [OculusField("is_archived")]
    [TrackChanges]
    public bool isArchived { get; set; } = false;
    [OculusFieldAlternate("bitfield_length")]
    [TrackChanges]
    public long? bitfieldLength { get; set; } = null;
    [OculusFieldAlternate("target_numerical")]
    [TrackChanges]
    public long? bitfieldTarget { get; set; } = null;
    [ListScrapingNodeFieldPresent]
    [TrackChanges]
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

    public DBAchievement GetEntryForDiffGeneration(IMongoCollection<DBAchievement> collection)
    {
        return collection.Find(x => x.id == this.id).FirstOrDefault();
    }

    public void AddOrUpdateEntry(IMongoCollection<DBAchievement> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions { IsUpsert = true });
    }

    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromGroupingId(grouping?.id ?? null);
    }

    public static List<DBAchievement> GetAllForApplicationGrouping(string? groupingId)
    {
        if (groupingId == null) return new List<DBAchievement>();
        return OculusDBDatabase.achievementCollection.Find(x => x.grouping != null && x.grouping.id == groupingId).ToList();
    }
}