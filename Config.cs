using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusDB
{
    public class Config
    {
        public string publicAddress { get; set; } = "";
        public int port { get; set; } = 504;
        public string mongoDBUrl { get; set; } = "";
        public string masterToken { get; set; } = "";
        public string mongoDBName { get; set; } = "OculusDB";
        public string masterWebhookUrl { get; set; } = "";
        public List<string> oculusTokens { get; set; } = new List<string> { "OC|1317831034909742|" };
        public int lastOculusToken { get; set; } = 0;
        public bool deleteOldData { get; set; } = true;
        public DateTime lastDBUpdate { get; set; } = DateTime.MinValue;
        public ScrapingResumeData ScrapingResumeData { get; set; } = new ScrapingResumeData();
        public List<Update> updates { get; set; } = new List<Update>();

        public static Config LoadConfig()
        {
            string configLocation = OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json";
            if (!File.Exists(configLocation)) File.WriteAllText(configLocation, JsonSerializer.Serialize(new Config()));
            return JsonSerializer.Deserialize<Config>(File.ReadAllText(configLocation));
        }

        public void Save()
        {
            File.WriteAllText(OculusDBEnvironment.workingDir + "data" + Path.DirectorySeparatorChar + "config.json", JsonSerializer.Serialize(this));
        }
    }

    public class ScrapingResumeData
    {
        public DateTime currentScrapeStart { get; set; } = DateTime.MinValue;
    }
}
