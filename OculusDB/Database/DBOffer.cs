using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBOffer : DBBase, IDBObjectOperations<DBOffer>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Offer;

    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBParentApplication? parentApplication { get; set; } = new DBParentApplication();
    [TrackChanges]
    public string id { get; set; } = "";
    [TrackChanges]
    public string currency { get; set; } = "";
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBPrice? price { get; set; } = null;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBPrice? strikethroughPrice { get; set; } = null;

    public DBOffer? GetEntryForDiffGeneration(IEnumerable<DBOffer> collection)
    {
        return collection.FirstOrDefault(x => x.currency == this.currency && x.id == this.id);
    }

    public void AddOrUpdateEntry(IMongoCollection<DBOffer> collection)
    {
        collection.ReplaceOne(x => x.currency == this.currency && x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
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
}