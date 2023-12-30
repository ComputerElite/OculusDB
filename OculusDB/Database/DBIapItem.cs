using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBIapItem : DBBase, IDBObjectOperations<DBIapItem>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IapItem;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("g")]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";

    [ListScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("a")]
    public List<DBAssetFile>? assetFiles { get; set; } = null;
    [OculusField("display_name")]
    [TrackChanges]
    [BsonElement("n")]
    public string displayName { get; set; } = "";
    [TrackChanges]
    [BsonElement("d")]
    public string displayShortDescription { get; set; } = "";
    [OculusField("is_cancelled")]
    [TrackChanges]
    [BsonElement("isc")]
    public bool? isCancelled { get; set; } = null;
    [OculusField("is_concept")]
    [TrackChanges]
    [BsonElement("isal")]
    public bool isAppLab { get; set; } = false;

    [OculusField("release_date_datetime")]
    [TrackChanges]
    [BsonElement("rd")]
    public DateTime? releaseDate { get; set; } = null;
    [OculusField("sku")]
    [TrackChanges]
    [BsonElement("sku")]
    public string? sku { get; set; } = null;
    [OculusFieldAlternate("iap_type_enum")]
    [TrackChanges]
    [BsonElement("it")]
    public IAPType iaptype { get; set; } = IAPType.UNKNOWN;
    [BsonIgnore]
    public string iapTypeFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(iaptype.ToString());
        }
    }
    
    // Specific Developer IAP request -> msrp_offers->nodes[0]->id
    [TrackChanges]
    [BsonElement("oi")]
    public string? offerId { get; set; } = null;
    [BsonIgnore]
    public List<DBOffer>? offers { get; set; } = null;

    public DBIapItem? GetEntryForDiffGeneration(IEnumerable<DBIapItem> collection)
    {
        return collection.FirstOrDefault(x => x.id == this.id);
    }

    public void AddOrUpdateEntry(IMongoCollection<DBIapItem> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }

    public Dictionary<string, string?> GetDiscordEmbedFields()
    {
        return new Dictionary<string, string?>
        {
            { "Display name", displayName},
            { "Display short description", displayShortDescription},
            { "SKU", sku},
            { "IAP Type", iapTypeFormatted},
            { "Release date", releaseDate?.ToString("yyyy-MM-dd") ?? null},
            { "Is cancelled", isCancelled?.ToString() ?? null},
            { "Is App Lab", isAppLab.ToString()},
        };
    }
    public Dictionary<string, string?> GetIdentifyDiscordEmbedFields()
    {
        return new Dictionary<string, string?>
        {
            { "Display name", displayName },
            { "SKU", sku}
        };
    }

    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromGroupingId(grouping?.id ?? null);
    }

    public override void PopulateSelf(PopulationContext context)
    {
        offers = context.GetOffers(offerId);
        if (grouping == null) return;
        grouping.applications = context.GetParentApplicationInApplicationGrouping(grouping.id);
    }

    public static List<DBIapItem> GetAllForApplicationGrouping(string? groupingId)
    {
        if (groupingId == null) return new List<DBIapItem>();
        return OculusDBDatabase.iapItemCollection.Find(x => x.grouping != null && x.grouping.id == groupingId).ToList();
    }

    public static DBIapItem? ById(string dId)
    {
        return OculusDBDatabase.iapItemCollection.Find(x => x.id == dId).FirstOrDefault();
    }

    public override string GetId()
    {
        return id;
    }
}