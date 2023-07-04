using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;

[BsonIgnoreExtraElements]
public class ScrapingProcessingStats
{
    public ScrapingNode scrapingNode { get; set; } = new ScrapingNode();
    public DateTime processStartTime { get; set; } = DateTime.MinValue;
    public DateTime processEndTime { get; set; } = DateTime.MinValue;
    public TimeSpan processTime
    {
        get
        {
            return processEndTime - processStartTime;
        }
    }
    public TimeSpan versionProcessTime { get; set; } = TimeSpan.Zero;
    public long versionsProcessed { get; set; } = 0;
    [BsonIgnore]
    public TimeSpan avgVersionProcessTime
    {
        get
        {
            if(versionsProcessed == 0)
                return TimeSpan.Zero;
            return versionProcessTime / versionsProcessed;
        }
    }
    [BsonIgnore]
    public double versionsProcessedPerSecond
    {
        get
        {
            if(avgVersionProcessTime.TotalSeconds == 0)
                return 0;
            return 1 / avgVersionProcessTime.TotalSeconds;
        }
    }

    public TimeSpan dlcProcessTime { get; set; } = TimeSpan.Zero;
    public long dlcsProcessed { get; set; } = 0;
    [BsonIgnore]
    public TimeSpan avgDlcProcessTime
    {
        get
        {
            if(dlcsProcessed == 0)
                return TimeSpan.Zero;
            return dlcProcessTime / dlcsProcessed;
        }
    }
    [BsonIgnore]
    public double dlcsProcessedPerSecond
    {
        get
        {
            if(avgDlcProcessTime.TotalSeconds == 0)
                return 0;
            return 1 / avgDlcProcessTime.TotalSeconds;
        }
    }
    
    public TimeSpan appProcessTime { get; set; } = TimeSpan.Zero;
    public long appsProcessed { get; set; } = 0;
    [BsonIgnore]
    public TimeSpan avgAppProcessTime
    {
        get
        {
            if(appsProcessed == 0)
                return TimeSpan.Zero;
            return appProcessTime / appsProcessed;
        }
    }
    [BsonIgnore]
    public double appsProcessedPerSecond
    {
        get
        {
            if(avgAppProcessTime.TotalSeconds == 0)
                return 0;
            return 1 / avgAppProcessTime.TotalSeconds;
        }
    }
    public TimeSpan dlcPackProcessTime { get; set; } = TimeSpan.Zero;
    public long dlcPacksProcessed { get; set; } = 0;
    [BsonIgnore]
    public TimeSpan avgDlcPackProcessTime
    {
        get
        {
            if(dlcPacksProcessed == 0)
                return TimeSpan.Zero;
            return dlcPackProcessTime / dlcPacksProcessed;
        }
    }
    [BsonIgnore]
    public double dlcPacksProcessedPerSecond
    {
        get
        {
            if(avgDlcPackProcessTime.TotalSeconds == 0)
                return 0;
            return 1 / avgDlcPackProcessTime.TotalSeconds;
        }
    }

    public long nodesProcessingAtStart { get; set; } = 0;
    public long nodesProcessingAtEnd { get; set; } = 0;
}