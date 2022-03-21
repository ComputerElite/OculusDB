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
        public ParentApplication parentApplication { get; set; } = new ParentApplication();

        // AndroidBinary
        public string id { get; set; } = "";
        public string version { get; set; } = "";
        public string platform { get; set; } = "";
        public string file_name { get; set; } = "";
        public long versionCode { get; set; } = 0;
        public long created_date { get; set; } = 0;
        public Nodes<ReleaseChannel> binary_release_channels { get; set; } = null;
        public Edges<Node<AppItemBundle>> firstIapItems { get; set; } = new Edges<Node<AppItemBundle>>();
    }
}
