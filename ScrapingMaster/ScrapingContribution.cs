namespace OculusDB.ScrapingMaster;

public class ScrapingContribution
{
    public Dictionary<string, long> contributionPerOculusDBType { get; set; } = new();
    public long appsQueuedForScraping { get; set; } = 0;
}