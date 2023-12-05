using MongoDB.Driver;
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

    public DBOffer GetEntryForDiffGeneration(IMongoCollection<DBOffer> collection)
    {
        return collection.Find(x => x.currency == this.currency && x.id == this.id).FirstOrDefault();
    }

    public void AddOrUpdateEntry(IMongoCollection<DBOffer> collection)
    {
        collection.ReplaceOne(x => x.currency == this.currency && x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }
    
    public override List<string> GetApplicationIds()
    {
        if (parentApplication == null) return new List<string>();
        return new List<string>() { parentApplication.id };
    }
}