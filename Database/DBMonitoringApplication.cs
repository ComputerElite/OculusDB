using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBMonitoringApplication
    {
        public string __OculusDBType { get; set; } = DBDataTypes.MonitoringApplication;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public List<string> monitoredVersionIDs { get; set; } = new List<string>();
        // Todo: Prices
    }
}
