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
    public class DBActivityPriceChanged
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
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityPriceChanged;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string oldPriceFormatted { get; set; } = "$0.00";
        public string oldPriceOffset { get; set; } = "0";
        public long oldPriceOffsetNumerical
        {
            get
            {
                return Convert.ToInt64(oldPriceOffset);
            }
            set
            {
                oldPriceOffset = value.ToString();
            }
        }
        public string newPriceFormatted { get; set; } = "$0.00";
        public string newPriceOffset { get; set; } = "0";
        public long newPriceOffsetNumerical
        {
            get
            {
                return Convert.ToInt64(newPriceOffset);
            }
            set
            {
                newPriceOffset = value.ToString();
            }
        }
        public string currency { get; set; } = "";
        public ParentApplication parentApplication { get; set; } = new ParentApplication();
    }
}
