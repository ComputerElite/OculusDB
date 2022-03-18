using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBActivityNewApplication
    {
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityNewApplication;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public DateTime releaseDate { get; set; } = DateTime.Now;
        public string displayName { get; set; } = "";
        public string publisher_name { get; set; } = "";
        public List<string> supported_hmd_platforms { get; set; } = new List<string>();
    }
}
