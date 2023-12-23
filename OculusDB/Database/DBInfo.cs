using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBInfo
    {
        public string scrapingStatusPageUrl { get; set; }
        public Dictionary<string, long> counts { get; set; } = new Dictionary<string, long>();
    }
}
