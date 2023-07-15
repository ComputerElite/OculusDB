using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.ScrapingMaster;


[BsonIgnoreExtraElements]
public class ScrapingNodeAuthenticationResult
{
    public string msg { get; set; } = "";
    public bool tokenAuthorized { get; set; } = false;
    public bool tokenExpired { get; set; } = true;
    public bool tokenValid { get; set; } = false;

    public string compatibleScrapingVersion { get; set; } = "";
    public ScrapingNodeOverrideSettings overrideSettings { get; set; } = new();

    public bool scrapingNodeVersionCompatible
    {
        get
        {
            return compatibleScrapingVersion == scrapingNode.scrapingNodeVersion;
        }
    }

    public ScrapingNode scrapingNode { get; set; } = new();
}