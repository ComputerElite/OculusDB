using System.Text.Json;
using System.Text.Json.Serialization;
using ComputerUtils.Logging;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ScrapingMaster;

namespace OculusDB.ScrapingNodeCode;


[BsonIgnoreExtraElements]
public class ScrapingNodeConfig
{
    private string _masterAddress = "";
    public string masterAddress
    {
        get
        {
            return _masterAddress.Trim('/');
        }
        set
        {
            _masterAddress = value;
        }
    }

    public string scrapingNodeToken { get; set; } = "";
    public string overrideCurrency { get; set; } = "";
    public List<string> oculusTokens { get; set; } = new List<string>();

    [JsonIgnore]
    public bool doForceScrape = false;
    [JsonIgnore]
    public bool isPriorityScrape = false;
    [JsonIgnore]
    public string appId = "";
    [JsonIgnore]
    public ScrapingNode scrapingNode;

    public static ScrapingNodeConfig LoadConfig()
    {
        Logger.Log("Loading scraping node config");
        string configLocation = OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "scrapingNodeConfig.json";
        if(!File.Exists(configLocation)) File.WriteAllText(configLocation, JsonSerializer.Serialize(new ScrapingNodeConfig()));
        try
        {
            return JsonSerializer.Deserialize<ScrapingNodeConfig>(File.ReadAllText(configLocation));
        }
        catch (Exception e)
        {
            Logger.Log("Couldn't load scraping node config", LoggingType.Error);
            return new ScrapingNodeConfig();
        }
    }

    public void Save()
    {
        string configLocation = OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "scrapingNodeConfig.json";
        File.WriteAllText(configLocation, JsonSerializer.Serialize(this));
    }
}