using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
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
    public List<DBPrice>? prices { get; set; } = null;

    public DBIAPItem GetEntryForDiffGeneration(IMongoCollection<DBIAPItem> collection)
    {
        return collection.Find(x => x.id == this.id).FirstOrDefault();
    }

    public void AddOrUpdateEntry(IMongoCollection<DBIAPItem> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }
}