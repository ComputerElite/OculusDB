using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingNodeStats
{
    public DateTime lastRestart { get; set; } = DateTime.MinValue;
    public DateTime lastHeartBeat { get; set; } = DateTime.MinValue;
    public ScrapingNode scrapingNode { get; set; } = new ScrapingNode();
    public int tokenCount { get; set; } = 0;
    public ScrapingContribution contribution { get; set; } = new ScrapingContribution();
    public ScrapingNodeSnapshot snapshot { get; set; } = new ScrapingNodeSnapshot();

    public ScrapingNodeStatus status
    {
        get
        {
            return snapshot.scrapingStatus;
        }
    }

    [BsonIgnore]
    public string statusString
    {
        get
        {
            return Enum.GetName(typeof(ScrapingNodeStatus), status);
        }
    }

    public DateTime firstSight { get; set; } = DateTime.MaxValue;
    public DateTime lastContribution { get; set; } = DateTime.MinValue;
    public TimeSpan runtime { get; set; } = TimeSpan.Zero;
    public TimeSpan totalRuntime { get; set; } = TimeSpan.Zero;

    [BsonIgnore]
    public bool online
    {
        get
        {
            int timeNeededForOffline = status == ScrapingNodeStatus.TransmittingResults ? 20 : 3;
            return DateTime.Now - lastHeartBeat < TimeSpan.FromMinutes(timeNeededForOffline);
        }
    }
}