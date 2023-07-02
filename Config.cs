using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OculusDB.ScrapingMaster;

namespace OculusDB
{
    public class Config
    {
        public OculusDBServerType serverType { get; set; } = OculusDBServerType.Frontend;
        public string scrapingMasterUrl { get; set; } = "";

        public string accesscode { get; set; } = "";

        public string publicAddress { get; set; } = "";
        public int port { get; set; } = 504;
        public string mongoDBUrl { get; set; } = "";
        public string masterToken { get; set; } = "";
        public string mongoDBName { get; set; } = "OculusDB";
        public string masterWebhookUrl { get; set; } = "";
        public string nodeStatusWebhookUrl { get; set; } = "";
        public List<Token> tokens { get; set; } = new List<Token>();

        public static ReaderWriterLock locker = new ReaderWriterLock();

        public static Config LoadConfig()
        {
            string configLocation = OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json";
            // make sure fallbacklocation.txt exists
            if(!File.Exists("fallbacklocation.txt")) File.WriteAllText("fallbacklocation.txt", "haehguihguioshgioueshrgrioushgsoighuesihgiesougheisoghoesiughesioughesuiogohes");
            
            // If config doesn't exist
            if (!File.Exists(configLocation))
            {
                if (File.Exists(File.ReadAllText("fallbacklocation.txt")))
                {
                    // Copy fallback config if it exist
                    Logger.Log("Config doesn't exist, using fallback config", LoggingType.Warning);
                    File.Copy(File.ReadAllText("fallbacklocation.txt"), configLocation, true);
                }
                else
                {
                    // or create default config
                    File.WriteAllText(configLocation, JsonSerializer.Serialize(new Config()));
                }
            }
            try
            {
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(configLocation));
            }
            catch (Exception e)
            {
                Logger.Log("Couldn't load config. Using fallback config if existing.", LoggingType.Error);
                File.Copy(File.ReadAllText("fallbacklocation.txt"), configLocation, true);
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(configLocation));
            }
        }

        public void Save()
        {
            try
            {
                // Acquire a writer lock to make sure no other thread is writing to the file
                locker.AcquireWriterLock(10000);
                File.WriteAllText(OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json", JsonSerializer.Serialize(this));
            }
            catch(Exception e)
            {
                Logger.Log("couldn't save config: " + e.ToString(), LoggingType.Warning);
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }
    }

    public enum OculusDBServerType
    {
        Frontend,
        ScrapeMaster,
        ScrapeNode
    }

    public class Token
    {
        public string token { get; set; } = "";
        public DateTime expiry { get; set; } = DateTime.UtcNow;
        public List<Permission> permissions { get; set; } = new();
    }

    public enum Permission
    {
        StartScrapes,
        StartPriorityScrapes,
        BlockApps,
        CreateScrapingNode
    }

    public enum ScrapingStatus
    {
        NotStarted,
        Starting,
        Running,
        Paused
    }

    public class ScrapingResumeData
    {
        public DateTime currentScrapeStart { get; set; } = DateTime.MinValue;
        public long appsToScrape { get; set; } = 0;
        public long scrapedApps { get; set; } = 0;
    }
}
