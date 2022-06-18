using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OculusDB.Analytics
{
    [BsonIgnoreExtraElements]
    public class Analytic
    {
        public string itemId { get; set; } = "";
        public string parentId { get; set; } = "";
        public long count { get; set; } = 0;
        [JsonIgnore]
        public DateTime reported { get; set; } = DateTime.UtcNow;
        public string applicationName { get; set; } = "";
    }
}
