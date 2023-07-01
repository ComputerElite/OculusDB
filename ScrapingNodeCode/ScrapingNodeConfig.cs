using System.Text.Json;
using ComputerUtils.Logging;
using MongoDB.Bson.Serialization.Attributes;

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
    public List<string> oculusTokens { get; set; } = new List<string>();
    
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