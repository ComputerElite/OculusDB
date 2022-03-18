using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database
{
    public class DBIAPItemPack : AppItemBundle
    {
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string __OculusDBType { get; set; } = DBDataTypes.IAPItemPack;
    }
}
