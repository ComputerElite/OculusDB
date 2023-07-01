using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBActivityVersionUpdated
    {
        [BsonIgnore]
        public string __id
        {
            get
            {
                return _id;
            }
        }
        /// <summary>
        /// Scraping node ID
        /// </summary>
        public string __sn { get; set; } = "";
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonIgnore]
        public string _id { get; set; }
        public string __lastEntry { get; set; } = null;
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityVersionUpdated;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public DateTime uploadedTime { get; set; } = DateTime.Now;
        public string version { get; set; } = "";
        public long versionCode { get; set; } = 0;
        public string changeLog { get; set; } = null;
        [BsonIgnore]
        public bool downloadable
        {
            get
            {
                return releaseChannels != null && releaseChannels.Count > 0;
            }
        }
        public List<ReleaseChannel> releaseChannels { get; set; } = new List<ReleaseChannel>();
        public ParentApplication parentApplication { get; set; } = new ParentApplication();
    }

    public class DBReleaseChannel
    {
        public string id { get; set; } = "";
        public string channel_name { get; set; } = "";

        public static explicit operator DBReleaseChannel(ReleaseChannel r)
        {
            DBReleaseChannel d = new DBReleaseChannel();
            d.id = r.id;
            d.channel_name = r.channel_name;
            return d;
        }
    }
}
