using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBVersion
    {
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string __OculusDBType { get; set; } = DBDataTypes.Version;
        /// <summary>
        /// Scraping node ID
        /// </summary>
        public string __sn { get; set; } = "";

        public HeadsetBinaryType binaryType { get; set; } = HeadsetBinaryType.Unknown;
        public ParentApplication parentApplication { get; set; } = new ParentApplication();

        // OculusBinary
        public string id { get; set; } = "";
        public string version { get; set; } = "";
		public string alias { get; set; } = null;
		public string changeLog { get; set; } = null;
        public string file_name { get; set; } = "";
        public long versionCode { get; set; } = 0;
        public long created_date { get; set; } = 0;
        public DateTime lastScrape { get; set; } = DateTime.MinValue;
        public DateTime lastPriorityScrape { get; set; } = DateTime.MinValue;
        public List<OBBBinary> obbList { get; set; } = null;
        public Nodes<ReleaseChannelWithoutLatestSupportedBinary> binary_release_channels { get; set; } = null;
        [BsonIgnore]
        public bool downloadable { get
            {
                return binary_release_channels != null && binary_release_channels.nodes.Count > 0;
            } }
        public Edges<Node<AppItemBundle>> firstIapItems { get; set; } = new Edges<Node<AppItemBundle>>();
    }

    public class OBBBinary
    {
        public string file_name { get; set; } = "";
        public string uri { get; set; } = "";
        public string size { get; set; } = "0";
        public string id { get; set; } = "";
        public bool is_required { get; set; } = false;
        public long sizeNumerical
        {
            get { return long.Parse(size); }
        }
        public string sizeString
        {
            get
            {
                return SizeConverter.ByteSizeToString(sizeNumerical);
            }
        }
    }
}
