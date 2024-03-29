using System.Linq.Expressions;
using System.Text.Json.Serialization;
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
    [BsonElement("g")]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    
    [OculusField("api_name")]
    [TrackChanges]
    [BsonElement("an")]
    public string apiName { get; set; } = "";
    [OculusFieldAlternate("achievement_type_enum")]
    [TrackChanges]
    [BsonElement("at")]
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
    [BsonElement("awp")]
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
    [BsonElement("isd")]
    public bool isDraft { get; set; } = false;
    [OculusField("is_secret")]
    [TrackChanges]
    [BsonElement("iss")]
    public bool isSecret { get; set; } = false;
    [OculusField("is_archived")]
    [TrackChanges]
    [BsonElement("isa")]
    public bool isArchived { get; set; } = false;
    [OculusFieldAlternate("bitfield_length")]
    [TrackChanges]
    [BsonElement("bl")]
    public long? bitfieldLength { get; set; } = null;
    [OculusFieldAlternate("target_numerical")]
    [TrackChanges]
    [BsonElement("bt")]
    public long? bitfieldTarget { get; set; } = null;
    [ListScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("t")]
    public List<DBAchievementTranslation> translations { get; set; } = new List<DBAchievementTranslation>();

    [JsonIgnore]
    [BsonElement("s")]
    public string? searchTitle { get; set; } = null;
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

    public DBAchievement? GetEntryForDiffGeneration(IEnumerable<DBAchievement> collection)
    {
        return collection.FirstOrDefault(x => x.id == this.id);
    }

    public void AddOrUpdateEntry(IMongoCollection<DBAchievement> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions { IsUpsert = true });
    }

    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromGroupingId(grouping?.id ?? null);
    }
    
    public override void PopulateSelf(PopulationContext context)
    {
        if (grouping == null) return;
        grouping.applications = context.GetParentApplicationInApplicationGrouping(grouping.id);
    }

    public static List<DBAchievement> GetAllForApplicationGrouping(string? groupingId)
    {
        if (groupingId == null) return new List<DBAchievement>();
        return OculusDBDatabase.achievementCollection.Find(x => x.grouping != null && x.grouping.id == groupingId).ToList();
    }

    public static DBAchievement? ById(string dId)
    {
        return OculusDBDatabase.achievementCollection.Find(x => x.id == dId).FirstOrDefault();
    }

    public override string GetId()
    {
        return id;
    }
}