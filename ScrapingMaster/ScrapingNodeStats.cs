namespace OculusDB.ScrapingMaster;

public class ScrapingNodeStats
{
    public ScrapingNode scrapingNode { get; set; } = new ScrapingNode();
    public int tokenCount { get; set; } = 0;
    public ScrapingContribution contribution { get; set; } = new ScrapingContribution();
    public ScrapingNodeStatus status { get; set; } = ScrapingNodeStatus.Idling;
    public DateTime firstSight { get; set; } = DateTime.MaxValue;
    public DateTime lastContribution { get; set; } = DateTime.MinValue;
    public TimeSpan runtime { get; set; } = TimeSpan.Zero;
}