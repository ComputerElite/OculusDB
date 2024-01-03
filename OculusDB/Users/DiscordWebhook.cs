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
            meta.Add("object id", OculusConverter.FormatDBEnumString(difference.entryId)); // add new line to add seperation
            embed.description += FormatDisct(meta);
            meta.Clear();
            if (difference.differenceType == DifferenceType.ObjectUpdated)
            {
                embed.description += FormatDisct(GetIdentifyDiscordEmbedFields(difference)) + "\n";
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
                embed.description += "\n";
                foreach (KeyValuePair<string, string?> field in GetNewDiscordEmbedFields(difference))
                {
                    meta.Add(field.Key, field.Value);
                }
            }

            embed.description += FormatDict(meta);
			
            embed.description += "\n**Difference link:** " + websiteUrl + "/difference/" + difference.__id;
            webhook.SendEmbed(embed, "OculusDB", icon);
            Thread.Sleep(1500);
        }

        public string FormatDict(Dictionary<string, string?> meta) {
            string s = "";
            foreach (KeyValuePair<string, string?> item in meta)
            {
                string toAdd = "**" + item.Key + ":** `" + (string.IsNullOrEmpty(item.Value) ? "-" : item.Value) + "`\n";
                if (s.Length + toAdd.Length >= 3500)
                {
                    s += "...\n";
                    break;
                }
                s += toAdd;
            }
            return s;
        }

        public Dictionary<string, string?> GetNewDiscordEmbedFields(DBDifference difference)
        {
            switch (difference.entryOculusDBType)
            {
                case DBDataTypes.Application:
                    return ((IDBObjectOperations<DBApplication>)difference.newObject).GetDiscordEmbedFields();
                case DBDataTypes.IapItem:
                    return ((IDBObjectOperations<DBIapItem>)difference.newObject).GetDiscordEmbedFields();
                case DBDataTypes.IapItemPack:
                    return ((IDBObjectOperations<DBIapItemPack>)difference.newObject).GetDiscordEmbedFields();
                case DBDataTypes.Achievement:
                    return ((IDBObjectOperations<DBAchievement>)difference.newObject).GetDiscordEmbedFields();
                case DBDataTypes.Version:
                    return ((IDBObjectOperations<DBVersion>)difference.newObject).GetDiscordEmbedFields();
                case DBDataTypes.AppImage:
                    return ((IDBObjectOperations<DBAppImage>)difference.newObject).GetDiscordEmbedFields();
                case DBDataTypes.Offer:
                    return ((IDBObjectOperations<DBOffer>)difference.newObject).GetDiscordEmbedFields();
            }

            return new Dictionary<string, string?>
            {
                { "Error", "Unknown object type" }
            };
        }
        
        public Dictionary<string, string?> GetIdentifyDiscordEmbedFields(DBDifference difference)
        {
            switch (difference.entryOculusDBType)
            {
                case DBDataTypes.Application:
                    return ((IDBObjectOperations<DBApplication>)difference.newObject).GetIdentifyDiscordEmbedFields();
                case DBDataTypes.IapItem:
                    return ((IDBObjectOperations<DBIapItem>)difference.newObject).GetIdentifyDiscordEmbedFields();
                case DBDataTypes.IapItemPack:
                    return ((IDBObjectOperations<DBIapItemPack>)difference.newObject).GetIdentifyDiscordEmbedFields();
                case DBDataTypes.Achievement:
                    return ((IDBObjectOperations<DBAchievement>)difference.newObject).GetIdentifyDiscordEmbedFields();
                case DBDataTypes.Version:
                    return ((IDBObjectOperations<DBVersion>)difference.newObject).GetIdentifyDiscordEmbedFields();
                case DBDataTypes.AppImage:
                    return ((IDBObjectOperations<DBAppImage>)difference.newObject).GetIdentifyDiscordEmbedFields();
                case DBDataTypes.Offer:
                    return ((IDBObjectOperations<DBOffer>)difference.newObject).GetIdentifyDiscordEmbedFields();
            }

            return new Dictionary<string, string?>
            {
                { "Error", "Unknown object type" }
            };
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
