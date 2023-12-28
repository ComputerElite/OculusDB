using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBOffer : DBBase, IDBObjectOperations<DBOffer>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Offer;

    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("p")]
    public DBParentApplication? parentApplication { get; set; } = new DBParentApplication();
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [TrackChanges]
    [BsonElement("c")]
    public string currency { get; set; } = "";
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("pr")]
    public DBPrice? price { get; set; } = null;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("s")]
    public DBPrice? strikethroughPrice { get; set; } = null;
    
    [TrackChanges]
    [BsonElement("o")]
    public List<string> presentOn { get; set; } = new List<string>();

    public DBOffer? GetEntryForDiffGeneration(IEnumerable<DBOffer> collection)
    {
        return collection.FirstOrDefault(x => x.currency == this.currency && x.id == this.id);
    }

    public void AddOrUpdateEntry(IMongoCollection<DBOffer> collection)
    {
        collection.ReplaceOne(x => x.currency == this.currency && x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }

    public Dictionary<string, string?> GetDiscordEmbedFields()
    {
        return new Dictionary<string, string?>
        {
            { "Currency", currency},
            { "Price", price?.priceFormatted ?? "N/A"},
            { "Strikethrough price", strikethroughPrice?.priceFormatted ?? "No strikethrough price"},
            { "For apps", string.Join(", ", presentOn)},
        };
    }

    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromAppId(parentApplication?.id ?? null);
    }

    public static List<DBOffer> GetAllForApplication(string parentApp)
    {
        return OculusDBDatabase.offerCollection.Find(x => x.parentApplication != null && x.parentApplication.id == parentApp).ToList();
    }

    public static List<DBOffer> ById(string id)
    {
        return OculusDBDatabase.offerCollection.Find(x => x.id == id).ToList();
    }

    public override string GetId()
    {
        return id;
    }
}