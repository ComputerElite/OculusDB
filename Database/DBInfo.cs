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
        public long dataDocuments { get; set; } = 0;
        public long activityDocuments { get; set; } = 0;
        public long appCount { get; set; } = 0;
    }
}
