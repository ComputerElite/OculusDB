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

namespace OculusDB.Users
{
    [BsonIgnoreExtraElements]
    public class DiscordActivityWebhook
    {
        [BsonIgnore]
        public Config config { get
            {
                return OculusDBEnvironment.config;
            } }
        public string url { get; set; } = "";
        public string applicationId { get; set; } = "";
        public List<string> activities { get; set; } = new List<string>();

        public void SendWebhook(BsonDocument activity)
        {
            string type = activity["__OculusDBType"].ToString();
            if (!activities.Contains(type)) return;
            DiscordWebhook webhook = new DiscordWebhook(url);
            DiscordEmbed embed = new DiscordEmbed();
            string websiteUrl = config.publicAddress;
            string icon = websiteUrl + "logo.png";
            embed.author = new DiscordEmbedAuthor { icon_url = icon, name = "OculusDB", url = websiteUrl };
            Dictionary<string, string> meta = new Dictionary<string, string>();
            string id = "";
            if(type == DBDataTypes.ActivityNewApplication)
            {
                DBActivityNewApplication app = ObjectConverter.ConvertToDBType(activity);
                embed.title = "New Application released";
                meta.Add("Name", app.displayName);
                meta.Add("Price", app.priceFormatted);
                meta.Add("Id", app.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(app.hmd));
                meta.Add("Publisher", app.publisherName);
                id = app.id;
            }
            else if (type == DBDataTypes.ActivityPriceChanged)
            {
                DBActivityPriceChanged app = ObjectConverter.ConvertToDBType(activity);
                embed.title = "Application price change";
                meta.Add("Name", app.parentApplication.displayName);
                meta.Add("New price", app.newPriceFormatted);
                meta.Add("Old price", app.oldPriceFormatted);
                meta.Add("Id", app.parentApplication.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(app.parentApplication.hmd));
                id = app.parentApplication.id;
            }
            else if (type == DBDataTypes.ActivityNewVersion)
            {
                DBActivityNewVersion v = ObjectConverter.ConvertToDBType(activity);
                embed.title = "New Version released";
                meta.Add("Version", v.version);
                meta.Add("Version code", v.versionCode.ToString());
                meta.Add("Downloadable", (v.releaseChannels.Count != 0).ToString());
                meta.Add("Release channels", String.Join(", ", v.releaseChannels));
                meta.Add("Id", v.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(v.parentApplication.hmd));
                meta.Add("Application", v.parentApplication.displayName);
                meta.Add("Application id", v.parentApplication.id);
                id = v.parentApplication.id;
            }
            else if (type == DBDataTypes.ActivityVersionUpdated)
            {
                DBActivityVersionUpdated v = ObjectConverter.ConvertToDBType(activity);
                embed.title = "Version updated";
                meta.Add("Version", v.version);
                meta.Add("Version code", v.versionCode.ToString());
                meta.Add("Downloadable", (v.releaseChannels.Count != 0).ToString());
                meta.Add("Release channels", String.Join(", ", v.releaseChannels));
                meta.Add("Id", v.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(v.parentApplication.hmd));
                meta.Add("Application", v.parentApplication.displayName);
                meta.Add("Application id", v.parentApplication.id);
                id = v.parentApplication.id;
            }
            else if (type == DBDataTypes.ActivityNewDLC)
            {
                DBActivityNewDLC v = ObjectConverter.ConvertToDBType(activity);
                embed.title = "New DLC released";
                meta.Add("DLC name", v.displayName);
                meta.Add("Price", v.priceFormatted);
                meta.Add("Id", v.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(v.parentApplication.hmd));
                meta.Add("Application", v.parentApplication.displayName);
                meta.Add("Application id", v.parentApplication.id);
                id = v.parentApplication.id;
            }
            else if (type == DBDataTypes.ActivityDLCUpdated)
            {
                DBActivityDLCUpdated v = ObjectConverter.ConvertToDBType(activity);
                embed.title = "DLC updated";
                meta.Add("DLC name", v.displayName);
                meta.Add("Price", v.priceFormatted);
                meta.Add("Id", v.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(v.parentApplication.hmd));
                meta.Add("Application", v.parentApplication.displayName);
                meta.Add("Application id", v.parentApplication.id);
                id = v.parentApplication.id;
            }
            else if (type == DBDataTypes.ActivityNewDLCPack)
            {
                DBActivityNewDLCPack v = ObjectConverter.ConvertToDBType(activity);
                embed.title = "New DLC Pack released";
                meta.Add("DLC Pack name", v.displayName);
                meta.Add("Price", v.priceFormatted);
                meta.Add("Included DLCs", "See on OculusDB website");
                meta.Add("Id", v.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(v.parentApplication.hmd));
                meta.Add("Application", v.parentApplication.displayName);
                meta.Add("Application id", v.parentApplication.id);
                id = v.parentApplication.id;
            }
            else if (type == DBDataTypes.ActivityDLCPackUpdated)
            {
                DBActivityDLCPackUpdated v = ObjectConverter.ConvertToDBType(activity);
                embed.title = "DLC Pack updated";
                meta.Add("DLC Pack name", v.displayName);
                meta.Add("Price", v.priceFormatted);
                meta.Add("Included DLCs", "See on OculusDB website");
                meta.Add("Id", v.id);
                meta.Add("Headset", HeadsetTools.GetHeadsetDisplayName(v.parentApplication.hmd));
                meta.Add("Application", v.parentApplication.displayName);
                meta.Add("Application id", v.parentApplication.id);
                id = v.parentApplication.id;
            }
            if (applicationId != "" && applicationId != id || applicationId == "" && (type == DBDataTypes.ActivityNewVersion || type == DBDataTypes.ActivityVersionUpdated)) return;
            foreach (KeyValuePair<string, string> item in meta)
            {
                embed.description += "**" + item.Key + ":** `" + (item.Value.Length <= 0 ? "none" : item.Value) + "`\n";
            }
            embed.description += "**Activity link:** " + websiteUrl + "activity/" + activity["_id"].ToString();
            webhook.SendEmbed(embed, "OculusDB", icon);
            Thread.Sleep(1500);
        }
    }
}
