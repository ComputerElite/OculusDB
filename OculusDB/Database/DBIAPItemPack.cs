using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBIAPItemPack : DBBase, IDBObjectOperations<DBIAPItemPack>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IapItemPack;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("g")]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [TrackChanges]
    [BsonElement("oi")]
    public string offerId { get; set; } = "";
    [BsonIgnore]
    public List<DBOffer>? offers { get; set; } = null;
    
    [OculusField("display_name")]
    [TrackChanges]
    [BsonElement("n")]
    public string displayName { get; set; } = "";
    [OculusField("display_short_description")]
    [TrackChanges]
    [BsonElement("d")]
    public string displayShortDescription { get; set; } = "";
    [TrackChanges]
    [BsonElement("i")]
    public List<DBIAPItemId> items { get; set; } = new List<DBIAPItemId>();

    public DBIAPItemPack? GetEntryForDiffGeneration(IEnumerable<DBIAPItemPack> collection)
    {
        return collection.FirstOrDefault(x => x.id == this.id);
    }

    public void AddOrUpdateEntry(IMongoCollection<DBIAPItemPack> collection)
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
        if (grouping == null) return;
        grouping.applications = context.GetParentApplicationInApplicationGrouping(grouping.id);
    }

    public static List<DBIAPItemPack> GetAllForApplicationGrouping(string? groupingId)
    {
        if (groupingId == null) return new List<DBIAPItemPack>();
        return OculusDBDatabase.iapItemPackCollection.Find(x => x.grouping != null && x.grouping.id == groupingId).ToList();
    }
    public static DBIAPItemPack? ById(string id)
    {
        return OculusDBDatabase.iapItemPackCollection.Find(x =>x.id == id).FirstOrDefault();
    }

    public override string GetId()
    {
        return id;
    }
}