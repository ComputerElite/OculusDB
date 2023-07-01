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
    public ScrapingContribution contribution { get; set; } = new ScrapingContribution();
    public ScrapingNodeSnapshot snapshot { get; set; } = new ScrapingNodeSnapshot();

    public ScrapingNodeStatus status
    {
        get
        {
            
            return online ? snapshot.scrapingStatus : ScrapingNodeStatus.Offline;
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
            if (snapshot.scrapingStatus == ScrapingNodeStatus.TransmittingResults)
            {
                if (ScrapingManaging.processingRn.ContainsKey(scrapingNode.scrapingNodeId))
                {
                    if (ScrapingManaging.processingRn[scrapingNode.scrapingNodeId].processing)
                    {
                        // Server is processing it rn
                        return DateTime.UtcNow - lastHeartBeat < TimeSpan.FromMinutes(30);
                    }
                    // Server is already done processing but node says it's transmitting results.
                    // It should take no longer than 20 seconds till the node reports another status.
                    return DateTime.UtcNow -
                           ScrapingManaging.processingRn[scrapingNode.scrapingNodeId].processingDone <
                           TimeSpan.FromSeconds(20);
                }
            }
            return DateTime.UtcNow - lastHeartBeat < TimeSpan.FromMinutes(1);
        }
    }
}