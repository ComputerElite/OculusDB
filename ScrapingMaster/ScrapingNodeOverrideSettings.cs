namespace OculusDB.ScrapingMaster;

public class ScrapingNodeOverrideSettings
{
    public ScrapingNode scrapingNode { get; set; } = new();
    public string overrideCurrency { get; set; } = "";
}