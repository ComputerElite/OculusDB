using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using OculusGraphQLApiLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBActivityNewApplication
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
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityNewApplication;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public DateTime releaseDate { get; set; } = DateTime.Now;
        public string displayName { get; set; } = "";
        public string displayLongDescription { get; set; } = "";
        public string publisherName { get; set; } = "";
        public string priceFormatted { get; set; } = "$0.00";
        public string priceOffset { get; set; } = "0";
        public long priceOffsetNumerical
        {
            get
            {
                return Convert.ToInt64(priceOffset);
            }
            set
            {
                priceOffset = value.ToString();
            }
        }
        public List<string> supportedHmdPlatforms { get; set; } = new List<string>();
        public Headset hmd { get; set; } = Headset.RIFT;
    }
}
