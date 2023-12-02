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

        public void SendOculusDBWebhook(BsonDocument activity)
        {
            if (!SendWebhook(activity)) return;
            string type = activity["__OculusDBType"].ToString();
            if(CheckDownloadableType(activity))
            {
                activity["__OculusDBType"] = DBDataTypes.ActivityVersionDownloadable;
            }
            WebClient c = new WebClient();
            c.Headers.Add("user-agent", OculusDBEnvironment.userAgent);
            c.UploadString(url, "POST", JsonSerializer.Serialize(ObjectConverter.ConvertToDBType(activity)));
        }

        public bool CheckDownloadableType(BsonDocument activity)
        {
            string type = activity["__OculusDBType"].ToString();
            return false;
        }

        public bool SendWebhook(BsonDocument activity)
        {
            string type = activity["__OculusDBType"].ToString();
            if (!activities.Contains(type) && !CheckDownloadableType(activity)) return false;
            string id;
            return true;
        }

        public void SendDiscordWebhook(BsonDocument activity)
        {
            if (!SendWebhook(activity)) return;
            string type = activity["__OculusDBType"].ToString();
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
            embed.description += "**Activity link:** " + websiteUrl + "activity/" + activity["_id"].ToString();
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
