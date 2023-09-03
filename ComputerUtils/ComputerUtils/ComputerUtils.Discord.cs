using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace ComputerUtils.Discord
{
    public class DiscordWebhook
    {
        public string url = "";

        public DiscordWebhook(string url)
        {
            this.url = url;
        }

        public void SendMessage(DiscordWebhookMessage msg)
        {
            WebClient c = new WebClient();
            c.Headers.Add("user-agent", "ComputerUtils/" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            c.Headers.Add("content-type", "application/json");
            c.UploadString(url, JsonSerializer.Serialize(msg));
            Logger.Log("sent message via Discord webhook");
        }

        public void SendMessage(string content, string username = "", string avatar = "")
        {
            SendMessage(new DiscordWebhookMessage { content = content, username = username, avatar_url = avatar });
        }

        public void SendEmbed(string titel, string description, string footer = "", string userName = "", string userIcon = "", string authorName = "", string authorIcon = "", string authorUrl = "", int color = 0xFFFFFF, string footerIcon = "", string imageUrl = "")
        {
            SendMessage(new DiscordWebhookMessage { username = userName, avatar_url = userIcon, embeds = new List<DiscordEmbed> { new DiscordEmbed() { color = color, title = titel, description = description, footer = new DiscordEmbedFooter { text = footer, icon_url = footerIcon }, image = new DiscordEmbedImage { url = imageUrl }, author = new DiscordEmbedAuthor { name = authorName, icon_url = authorIcon, url = authorUrl } } } });
        }

        public void SendEmbed(DiscordEmbed embed, string username, string userIcon)
        {
            SendMessage(new DiscordWebhookMessage { username = username, avatar_url = userIcon, embeds = new List<DiscordEmbed> { embed } });
        }
    }

    public class DiscordWebhookMessage
    {
        public string content { get; set; } = "";
        public string username { get; set; } = "";
        public string avatar_url { get; set; } = "";
        public bool tts { get; set; } = false;
        public List<DiscordEmbed> embeds { get; set; } = new List<DiscordEmbed>();

    }

    public class DiscordEmbed
    {
        public string title { get; set; } = "";
        public string description { get; set; } = "";
        public int color { get; set; } = 0xFFFFFF;
        public DiscordEmbedFooter footer { get; set; } = new DiscordEmbedFooter();
        public List<DiscordEmbedField> fields { get; set; } = new List<DiscordEmbedField>();
        public DiscordEmbedAuthor author { get; set; } = new DiscordEmbedAuthor();
        public DiscordEmbedImage image { get; set; } = new DiscordEmbedImage();
    }

    public class DiscordEmbedFooter
    {
        public string text { get; set; } = "";
        public string icon_url { get; set; } = "";
    }
    public class DiscordEmbedField
    {
        public string name { get; set; } = "";
        public string value { get; set; } = "";
        public bool inline { get; set; } = false;
    }
    public class DiscordEmbedImage
    {
        public string url { get; set; } = "";
    }
    public class DiscordEmbedAuthor
    {

        public string name { get; set; } = "";
        public string url { get; set; } = "";
        public string icon_url { get; set; } = "";
    }
}