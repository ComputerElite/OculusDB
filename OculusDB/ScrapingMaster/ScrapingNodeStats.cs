using ComputerUtils.Logging;
using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingNodeStats
{
    public DateTime lastRestart { get; set; } = DateTime.MinValue;
    public DateTime lastHeartBeat { get; set; } = DateTime.MinValue;
    public ScrapingNode scrapingNode { get; set; } = new ScrapingNode();
    public int tokenCount { get; set; } = 0;
    // Contribution is store in another collection
    [BsonIgnore]
    public ScrapingContribution contribution { get; set; } = new ScrapingContribution();
    public ScrapingNodeSnapshot snapshot { get; set; } = new ScrapingNodeSnapshot();

    public ScrapingNodeStatus status
    {
        get
        {
            
            return online || snapshot.scrapingStatus == ScrapingNodeStatus.OAuthException ? snapshot.scrapingStatus : ScrapingNodeStatus.Offline;
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
    public TimeSpan runtime { get; set; } = TimeSpan.Zero;
    public TimeSpan totalRuntime { get; set; } = TimeSpan.Zero;

    public bool online { get; set; } = false;
    public long tasksProcessing { get; set; } = 0;

    public void SetOnline()
    {
        online = DateTime.UtcNow - lastHeartBeat < TimeSpan.FromMinutes(1);
    }
}