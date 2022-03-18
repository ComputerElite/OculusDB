using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBActivityNewDLC
    {
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityNewDLC;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string id { get; set; } = "";
        public string displayName { get; set; } = "";
        public string displayShortDescription { get; set; } = "";

        public ParentApplication parentApplication { get; set; } = new ParentApplication();
    }
}
