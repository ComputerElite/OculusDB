using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusDB
{
    public class Config
    {
        public string accesscode { get; set; } = "";

        public string publicAddress { get; set; } = "";
        public string crashPingId { get; set; } = "631189193825058826";
        public int port { get; set; } = 504;
        public string mongoDBUrl { get; set; } = "";
        public string masterToken { get; set; } = "";
        public string mongoDBName { get; set; } = "OculusDB";
        public string masterWebhookUrl { get; set; } = "";
        public List<string> oculusTokens { get; set; } = new List<string> { "OC|1317831034909742|" };
        public int lastOculusToken { get; set; } = 0;
        public int lastValidToken { get; set; } = 0;
        public bool deleteOldData { get; set; } = true;
        public bool pauseAfterScrape { get; set; } = false;
        public DateTime lastDBUpdate { get; set; } = DateTime.MinValue;
        public ScrapingResumeData ScrapingResumeData { get; set; } = new ScrapingResumeData();
        public List<Update> updates { get; set; } = new List<Update>();
		public ScrapingStatus scrapingStatus { get; set; } = ScrapingStatus.NotStarted;

		public static Config LoadConfig()
        {
            string configLocation = OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json";
            if (!File.Exists(configLocation)) File.WriteAllText(configLocation, JsonSerializer.Serialize(new Config()));
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
                File.WriteAllText(OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json", JsonSerializer.Serialize(this));
            } catch(Exception e)
            {
                Logger.Log("couldn't save config: " + e.ToString(), LoggingType.Warning);
            }
        }
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
