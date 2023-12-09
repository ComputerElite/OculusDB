using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerUtils.Discord;
using OculusDB.Database;
using MongoDB.Bson.Serialization.Attributes;
using OculusGraphQLApiLib;
using System.Net;
using ComputerUtils.Logging;
using System.Text.Json;
using OculusDB.ObjectConverters;

namespace OculusDB.Users
{
    [BsonIgnoreExtraElements]
    public class ActivityWebhook
    {
        [BsonIgnore]
        public Config config { get
            {
                return OculusDBEnvironment.config;
            } }
        public string url { get; set; } = "";
        public string applicationId { get; set; } = "";
        public ActivityWebhookType type { get; set; } = ActivityWebhookType.Discord;
        public List<string> activities { get; set; } = new List<string>();

        public void SendOculusDBWebhook(DBDifference difference)
        {
            if (!SendWebhook(difference)) return;
            WebClient c = new WebClient();
            c.Headers.Add("user-agent", OculusDBEnvironment.userAgent);
            c.UploadString(url, "POST", JsonSerializer.Serialize(difference));
        }

        public bool CheckDownloadableType(BsonDocument activity)
        {
            string type = activity["__OculusDBType"].ToString();
            return false;
        }

        public bool SendWebhook(DBDifference difference)
        {
            
            return true;
        }

        public void SendDiscordWebhook(DBDifference difference)
        {
            if (!SendWebhook(difference)) return;
            DifferenceName type = difference.differenceNameEnum;
            DiscordWebhook webhook = new DiscordWebhook(url);
            DiscordEmbed embed = new DiscordEmbed();
            string websiteUrl = config.publicAddress;
            string icon = websiteUrl + "logo";
            embed.author = new DiscordEmbedAuthor { icon_url = icon, name = "OculusDB", url = websiteUrl };
            Dictionary<string, string> meta = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> item in meta)
            {
                embed.description += "**" + item.Key + ":** `" + (item.Value.Length <= 0 ? "none" : item.Value) + "`\n";
            }
            embed.description += "**Activity link:** " + websiteUrl + "activity/" + difference._id;
            webhook.SendEmbed(embed, "OculusDB", icon);
            Thread.Sleep(1200);
        }
    }

    public enum ActivityWebhookType
    {
        Discord = 0,
        OculusDB = 1
    }
}
