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
    public class DBActivityVersionChangelogAvailable
    {
        [BsonIgnore]
        public string __id
        {
            get
            {
                return _id;
            }
        }
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonIgnore]
        public string _id { get; set; }
        public string __lastEntry { get; set; } = null;
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityVersionChangelogAvailable;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public DateTime uploadedTime { get; set; } = DateTime.Now;
        public string version { get; set; } = "";
        public long versionCode { get; set; } = 0;
        public string changeLog { get; set; } = null;
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
}
