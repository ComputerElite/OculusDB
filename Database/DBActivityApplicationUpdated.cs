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
    public class DBActivityApplicationUpdated
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
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityApplicationUpdated;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public DBApplication oldApplication { get; set; } = new DBApplication();
        public DBApplication newApplication { get; set; } = new DBApplication();
    }
}
