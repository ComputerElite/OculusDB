using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBIAPItem : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItem;
    [ObjectScrapingNodeFieldPresent]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    public string id { get; set; } = "";
    [ListScrapingNodeFieldPresent]
    public List<DBAssetFile> assetFiles { get; set; } = new List<DBAssetFile>();
    [OculusField("display_name")]
    public string displayName { get; set; } = "";
    [OculusField("is_cancelled")]
    public bool isCancelled { get; set; } = false;
    [OculusField("is_concept")]
    public bool isAppLab { get; set; } = false;
    [OculusField("release_date_datetime")]
    public DateTime releaseDate { get; set; } = DateTime.MinValue;
    [OculusField("sku")]
    public string sku { get; set; } = "";
    [OculusFieldAlternate("iap_type_enum")]
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
    public string? offerId { get; set; } = null;
    [BsonIgnore]
    public List<DBPrice>? prices { get; set; } = null;
}