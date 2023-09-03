using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database
{
    public class DBIAPItemPack : IAPItem
    {
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string __OculusDBType { get; set; } = DBDataTypes.IAPItemPack;
        /// <summary>
        /// Scraping node ID
        /// </summary>
        public string __sn { get; set; } = "";

        // AppItemBundle
        [ObjectConverterIgnore]
        public List<DBItemId> bundle_items { get; set; } = new List<DBItemId>();
        public bool is_360_sensor_setup_required { get; set; } = false;
        public bool is_guardian_required { get; set; } = false;
        public bool is_roomscale_required { get; set; } = false;
        public bool is_touch_required { get; set; } = false;

        public override string ToString()
        {
            return "DLC Pack '" + display_name + "' (" + id + ") of " + parentApplication.id + "(" + bundle_items.Count + " bundle items)";
        }
    }
}
