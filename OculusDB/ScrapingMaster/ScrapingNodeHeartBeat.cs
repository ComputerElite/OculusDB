using System.Data;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.Database;
using OculusDB.ScrapingNodeCode;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingNodeHeartBeat
{
    public ScrapingNodeIdentification identification { get; set; } = new ScrapingNodeIdentification();
    public ScrapingNodeSnapshot snapshot { get; set; } = new ScrapingNodeSnapshot();

    public void SetQueuedDocuments(ScrapingNodeTaskResult taskResult)
    {
        snapshot.queuedDocuments[DBDataTypes.Application] = taskResult.scraped.applications.Count;
        snapshot.queuedDocuments[DBDataTypes.Version] = taskResult.scraped.versions.Count;
        snapshot.queuedDocuments[DBDataTypes.IAPItem] = taskResult.scraped.iapItems.Count;
        snapshot.queuedDocuments[DBDataTypes.IAPItemPack] = taskResult.scraped.iapItemPacks.Count;
        snapshot.queuedDocuments[DBDataTypes.AppImage] = taskResult.scraped.imgs.Count;
        snapshot.queuedDocuments[DBDataTypes.Achievement] = taskResult.scraped.achievements.Count;
        snapshot.queuedDocuments[DBDataTypes.Offer] = taskResult.scraped.offers.Count;
    }
}