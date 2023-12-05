using MongoDB.Driver;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBIAPItemPack : DBBase, IDBObjectOperations<DBIAPItemPack>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItemPack;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    [TrackChanges]
    public string offerId { get; set; } = "";
    [OculusField("display_name")]
    [TrackChanges]
    public string displayName { get; set; } = "";
    [OculusField("display_short_description")]
    [TrackChanges]
    public string displayShortDescription { get; set; } = "";
    [TrackChanges]
    public List<DBIAPItemId> items { get; set; } = new List<DBIAPItemId>();

    public DBIAPItemPack GetEntryForDiffGeneration(IMongoCollection<DBIAPItemPack> collection)
    {
        return collection.Find(x => x.id == this.id).FirstOrDefault();
    }

    public void AddOrUpdateEntry(IMongoCollection<DBIAPItemPack> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }
    public override List<string> GetApplicationIds()
    {
        return DBApplicationGrouping.GetApplicationIdsFromGrouping(grouping?.id ?? null);
    }
}