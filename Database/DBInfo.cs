using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBInfo
    {
        public string scrapingStatusPageUrl;
        public DateTime lastScrapeUpdate { get; set; } = DateTime.MinValue;
        public long dataDocuments { get; set; } = 0;
        public long activityDocuments { get; set; } = 0;
        public long appsToScrape { get; set; } = 0;
        public long scrapedApps { get; set; } = 0;
        public ScrapingStatus scrapingStatus { get; set; } = ScrapingStatus.Paused;
        public string scrapingStatusString { get
            {
                return Enum.GetName(scrapingStatus);
            } }
        public DateTime lastUpdated { get; set; } = DateTime.MinValue;
        public DateTime currentUpdateStart { get; set; } = DateTime.MinValue;
    }
}
