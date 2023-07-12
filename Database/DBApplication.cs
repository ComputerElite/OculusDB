using MongoDB.Bson.Serialization.Attributes;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBApplication
    {
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string __OculusDBType { get; set; } = DBDataTypes.Application;
        public Headset hmd { get; set; } = Headset.RIFT;
        /// <summary>
        /// Scraping node ID
        /// </summary>
        public string __sn { get; set; } = "";
        [BsonIgnore]

        public bool blocked
        {
            get
            {
                return MongoDBInteractor.GetBlockedStatusForApp(id);
            }
        }

        // Application
        public string appName { get; set; } = "";
        public AppStoreOffer baseline_offer { get; set; } = new AppStoreOffer();
        public string priceFormatted { get; set; } = "$0.00";
        public long priceOffsetNumerical { get; set; } = 0;
        public string currency { get; set; } = "";
        public string canonicalName { get; set; } = "";
        public AppStoreOffer current_gift_offer { get; set; } = new AppStoreOffer();
        public AppStoreOffer current_offer { get; set; } = new AppStoreOffer();
        [BsonIgnore]
        public string displayName { get { return display_name; } set { display_name = value; } }
        public string display_name { get; set; } = "";
        public string display_long_description { get; set; } = "";
        public List<string> genre_names { get; set; } = new List<string>();
        public bool has_in_app_ads { get; set; } = false;
        public string id { get; set; } = "";
        public bool is_approved { get; set; } = false;
        public bool is_concept { get; set; } = false; // aka AppLab
        public bool is_enterprise_enabled { get; set; } = false;
        public Organization organization { get; set; } = new Organization();
        public string platform { get; set; } = "";
        public string publisher_name { get; set; } = "";

        public double? quality_rating_aggregate { get; set; } = 0.0;

        public string img { get; set; } = "";
        public string packageName { get; set; } = "";

        [BsonIgnore]
        public string imageLink { get
            {
                return "/cdn/images/" + id;
            } }
        public Nodes<ReleaseChannel> release_channels { get; set; } = new Nodes<ReleaseChannel>();
        public long? release_date { get; set; } = 0;
        public List<string> supported_hmd_platforms { get; set; } = new List<string>();
        public List<Headset> supported_hmd_platforms_enum
        {
            get
            {
                List<Headset> headsets = new List<Headset>();
                foreach (string s in supported_hmd_platforms)
                {
                    headsets.Add((Headset)Enum.Parse(typeof(Headset), s));
                }
                return headsets;
            }
        }
        public string website_url { get; set; } = "";
    }
}
