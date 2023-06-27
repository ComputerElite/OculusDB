namespace OculusDB.ScrapingMaster;

public class ScrapingNodeStats
{
    public string scrapingNodeId { get; set; } = "";
    public string scrapingNodeName { get; set; } = "";
    public int tokenCount { get; set; } = 0;
    public ScrapingContribution contribution { get; set; } = new ScrapingContribution();
    public ScrapingNodeStatus status { get; set; } = ScrapingNodeStatus.Idling;
    public DateTime firstSight { get; set; } = DateTime.MaxValue;
    public TimeSpan runtime { get; set; } = TimeSpan.Zero;
}