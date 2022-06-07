using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBInfo
    {
        public long dataDocuments { get; set; } = 0;
        public long activityDocuments { get; set; } = 0;
        public long appsToScrape { get; set; } = 0;
        public long scrapedApps { get; set; } = 0;
        public DateTime lastUpdated { get; set; } = DateTime.MinValue;
        public DateTime currentUpdateStart { get; set; } = DateTime.MinValue;
    }
}
