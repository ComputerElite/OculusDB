namespace OculusDB.ScrapingMaster;

public class ScrapingNodeAuthenticationResult
{
    public string msg { get; set; } = "";
    public bool tokenAuthorized { get; set; } = false;
    public bool tokenExpired { get; set; } = true;
    public bool tokenValid { get; set; } = false;

    public string compatibleScrapingVersion
    {
        get { return OculusDBEnvironment.updater.version; }
    }

    public bool scrapingNodeVersionCompatible
    {
        get
        {
            return compatibleScrapingVersion == scrapingNode.scrapingNodeVersion;
        }
    }

    public ScrapingNode scrapingNode { get; set; } = new();
}