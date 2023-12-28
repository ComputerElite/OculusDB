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
using MongoDB.Bson.Serialization.IdGenerators;
using OculusDB.ObjectConverters;

namespace OculusDB.Users
{
    [BsonIgnoreExtraElements]
    public class DifferenceWebhook
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public string __id { get; set; } = "";
        [BsonIgnore]
        public Config config { get
            {
                return OculusDBEnvironment.config;
            } }
        public string url { get; set; } = "";
        public List<string> applicationIds { get; set; } = new List<string>();
        public DifferenceWebhookType type { get; set; } = DifferenceWebhookType.Discord;
        public List<DifferenceNameType> differenceTypes { get; set; } = new List<DifferenceNameType>();
        public string notes { get; set; } = "";
        

        public void SendOculusDbWebhook(DBDifference difference)
        {
            if (url == "") return;
            if (!SendWebhook(difference)) return;
            WebClient c = new WebClient();
            c.Headers.Add("user-agent", OculusDBEnvironment.userAgent);
            c.UploadString(url, "POST", JsonSerializer.Serialize(difference));
        }
        
        public bool SendWebhook(DBDifference difference)
        {
            if (!applicationIds.Any(x => difference.entryParentApplicationIds.Contains(x))) return false;
            if (!differenceTypes.Contains(difference.differenceName)) return false;
            return true;
        }

        public void SendDiscordWebhook(DBDifference difference)
        {
            if (url == "") return;
            if (!SendWebhook(difference)) return;
            DiscordWebhook webhook = new DiscordWebhook(url);
            DiscordEmbed embed = new DiscordEmbed();
            string websiteUrl = config.publicAddress;
            string icon = websiteUrl + "logo";
            embed.author = new DiscordEmbedAuthor { icon_url = icon, name = "OculusDB", url = websiteUrl };
            embed.title = OculusConverter.FormatDBEnumString(difference.differenceName.ToString());
            
            Dictionary<string, string?> meta = new Dictionary<string, string?>();
            meta.Add("diff type", OculusConverter.FormatDBEnumString(difference.differenceType.ToString()));
            if (difference.differenceType == DifferenceType.ObjectUpdated)
            {
                // When updated we should list all changes
                foreach (DBDifferenceEntry entry in difference.entries)
                {
                    string oldValue = entry.oldValue.ToString();
                    string newValue = entry.newValue.ToString();
                    if (oldValue.Length > 100) oldValue = oldValue.Substring(0, 100) + "...";
                    if (newValue.Length > 100) newValue = newValue.Substring(0, 100) + "...";
                    meta.Add(entry.name, oldValue + " -> " + newValue);
                }
            } else if (difference.differenceType == DifferenceType.ObjectAdded)
            {
                Dictionary<string, string?> fields = new Dictionary<string, string?>();
                // Get newObject as IDBObjectOperations if it implements the interface
                switch (difference.entryOculusDBType)
                {
                    case DBDataTypes.Application:
                        fields = ((IDBObjectOperations<DBApplication>)difference.newObject).GetDiscordEmbedFields();
                        break;
                    case DBDataTypes.IapItem:
                        fields = ((IDBObjectOperations<DBIapItem>)difference.newObject).GetDiscordEmbedFields();
                        break;
                    case DBDataTypes.IapItemPack:
                        fields = ((IDBObjectOperations<DBIapItemPack>)difference.newObject).GetDiscordEmbedFields();
                        break;
                    case DBDataTypes.Achievement:
                        fields = ((IDBObjectOperations<DBAchievement>)difference.newObject).GetDiscordEmbedFields();
                        break;
                    case DBDataTypes.Version:
                        fields = ((IDBObjectOperations<DBVersion>)difference.newObject).GetDiscordEmbedFields();
                        break;
                    case DBDataTypes.AppImage:
                        fields = ((IDBObjectOperations<DBAppImage>)difference.newObject).GetDiscordEmbedFields();
                        break;
                    case DBDataTypes.Offer:
                        fields = ((IDBObjectOperations<DBOffer>)difference.newObject).GetDiscordEmbedFields();
                        break;
                    
                }
                foreach (KeyValuePair<string, string?> field in fields)
                {
                    meta.Add(field.Key, field.Value);
                }
            }
			foreach (KeyValuePair<string, string?> item in meta)
            {
                string toAdd = "**" + item.Key + ":** `" + (string.IsNullOrEmpty(item.Value) ? "-" : item.Value) + "`\n";
                if (embed.description.Length + toAdd.Length >= 3500)
                {
                    embed.description += "...\n";
                    break;
                }
                embed.description += "**" + item.Key + ":** `" + (item.Value.Length <= 0 ? "none" : item.Value) + "`\n";
            }
            embed.description += "\n**Difference link:** " + websiteUrl + "difference/" + difference.__id;
            webhook.SendEmbed(embed, "OculusDB", icon);
            Thread.Sleep(1200);
        }
    }

    public class DifferenceWebhookResponse
    {
        public string msg { get; set; } = "";
        public bool isNewWebhook { get; set; } = false;
    }

    public enum DifferenceWebhookType
    {
        Discord = 0,
        OculusDB = 1
    }
}
