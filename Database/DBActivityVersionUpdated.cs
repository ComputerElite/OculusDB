using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBActivityVersionUpdated
    {
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityVersionUpdated;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public DateTime uploadedTime { get; set; } = DateTime.Now;
        public string version { get; set; } = "";
        public long versionCode { get; set; } = 0;
        public List<ReleaseChannel> releaseChannels { get; set; } = new List<ReleaseChannel>();
        public ParentApplication parentApplication { get; set; } = new ParentApplication();
    }
}
