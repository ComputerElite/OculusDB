using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBActivityNewDLCPack
    {
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityNewDLCPack;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public string displayName { get; set; } = "";
        public string displayShortDescription { get; set; } = "";

        public ParentApplication parentApplication { get; set; } = new ParentApplication();
        public List<DBActivityNewDLCPackDLC> includedDLCs { get; set; } = new List<DBActivityNewDLCPackDLC>();
    }

    public class DBActivityNewDLCPackDLC
    {
        public string id { get; set; } = "";
        public string displayName { get; set; } = "";
        public string shortDescription { get; set; } = "";
    }
}
