using System.Text.Json;
using ComputerUtils.Logging;

namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeConfig
{
    public string masterAddress
    {
        get
        {
            return masterAddress.Trim('/');
        }
        set
        {
            masterAddress = value;
        }
    }

    public string scrapingNodeToken { get; set; } = "";
    public List<string> oculusTokens { get; set; } = new List<string>();
    
    public static ScrapingNodeConfig LoadConfig()
    {
        string configLocation = OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "scrapingNodeConfig.json";
        try
        {
            return JsonSerializer.Deserialize<ScrapingNodeConfig>(File.ReadAllText(configLocation));
        }
        catch (Exception e)
        {
            Logger.Log("Couldn't load config", LoggingType.Error);
            return new ScrapingNodeConfig();
        }
    }

}