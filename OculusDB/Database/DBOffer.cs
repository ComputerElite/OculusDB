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
    [BsonElement("g")]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
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
            { "For ", string.Join(", ", presentOn)},
        };
    }
    public Dictionary<string, string?> GetIdentifyDiscordEmbedFields()
    {
        return new Dictionary<string, string?>
        {
            { "For ", string.Join(", ", presentOn)},
        };
    }

    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromGroupingId(grouping?.id ?? null);
    }

    public static List<DBOffer> ById(string id)
    {
        return OculusDBDatabase.offerCollection.Find(x => x.id == id).ToList();
    }

    public override void PopulateSelf(PopulationContext context)
    {
        if (grouping == null) return;
        grouping.applications = context.GetParentApplicationInApplicationGrouping(grouping.id);
    }

    public override string GetId()
    {
        return id;
    }

    public static List<DBOffer> GetAllForGrouping(string groupingId)
    {
        return OculusDBDatabase.offerCollection.Find(x => x.grouping != null && x.grouping.id == groupingId).ToList();
    }
}