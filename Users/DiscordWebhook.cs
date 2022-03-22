using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Users
{
    public class DiscordWebhook
    {
        public string url { get; set; } = "";
        public string applicationId { get; set; } = "";
        public string displayName { get; set; } = "";
        public List<string> activities { get; set; } = new List<string>();
    }
}
