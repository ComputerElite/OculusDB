using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBIAPItem : DBBase, IDBObjectOperations<DBIAPItem>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItem;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    [ListScrapingNodeFieldPresent]
    [TrackChanges]
    public List<DBAssetFile> assetFiles { get; set; } = new List<DBAssetFile>();
    [OculusField("display_name")]
    [TrackChanges]
    public string displayName { get; set; } = "";
    [TrackChanges]
    public string displayShortDescription { get; set; } = "";
    [OculusField("is_cancelled")]
    [TrackChanges]
    public bool isCancelled { get; set; } = false;
    [OculusField("is_concept")]
    [TrackChanges]
    public bool isAppLab { get; set; } = false;

    [OculusField("release_date_datetime")]
    [TrackChanges]
    public DateTime? releaseDate { get; set; } = null;
    [OculusField("sku")]
    [TrackChanges]
    public string sku { get; set; } = "";
    [OculusFieldAlternate("iap_type_enum")]
    [TrackChanges]
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
    public string? offerId { get; set; } = null;
    [BsonIgnore]
    public List<DBOffer>? offers { get; set; } = null;

    public DBIAPItem? GetEntryForDiffGeneration(IEnumerable<DBIAPItem> collection)
    {
        return collection.FirstOrDefault(x => x.id == this.id);
    }

    public void AddOrUpdateEntry(IMongoCollection<DBIAPItem> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }
    
    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromGroupingId(grouping?.id ?? null);
    }

    public override void PopulateSelf(PopulationContext context)
    {
        offers = context.GetOffers(offerId);
    }

    public static List<DBIAPItem> GetAllForApplicationGrouping(string? groupingId)
    {
        if (groupingId == null) return new List<DBIAPItem>();
        return OculusDBDatabase.iapItemCollection.Find(x => x.grouping != null && x.grouping.id == groupingId).ToList();
    }

    public static DBIAPItem? ById(string dId)
    {
        return OculusDBDatabase.iapItemCollection.Find(x => x.id == dId).FirstOrDefault();
    }

    public override string GetId()
    {
        return id;
    }
}