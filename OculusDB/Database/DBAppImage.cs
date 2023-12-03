using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

// Don't track app images

public class DBAppImage : DBBase, IDBObjectOperations<DBAppImage>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AppImage;
    [ObjectScrapingNodeFieldPresent]
    public DBParentApplication parentApplication { get; set; } = new DBParentApplication();
    public string mimeType { get; set; } = "image/webp";
    public byte[] data { get; set; } = new byte[0];
    public DBAppImage GetEntryForDiffGeneration(IMongoCollection<DBAppImage> collection)
    {
        return collection.Find(x => x.parentApplication.id == this.parentApplication.id).FirstOrDefault();
    }

    public void AddOrUpdateEntry(IMongoCollection<DBAppImage> collection)
    {
        collection.ReplaceOne(x => x.parentApplication.id == this.parentApplication.id, this, new ReplaceOptions() { IsUpsert = true });
    }
}