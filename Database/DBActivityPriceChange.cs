using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBActivityPriceChange
    {
        public string __OculusDBType { get; set; } = DBDataTypes.ActivityPriceChange;
        public DateTime __lastUpdated { get; set; } = DateTime.Now;
        public string oldPriceFormatted { get; set; } = "$0.00";
        public string oldPriceOffset { get; set; } = "0";
        public long oldPriceOffsetNumerical
        {
            get
            {
                return Convert.ToInt64(oldPriceOffset);
            }
            set
            {
                oldPriceOffset = value.ToString();
            }
        }
        public string newPriceFormatted { get; set; } = "$0.00";
        public string newPriceOffset { get; set; } = "0";
        public long newPriceOffsetNumerical
        {
            get
            {
                return Convert.ToInt64(newPriceOffset);
            }
            set
            {
                newPriceOffset = value.ToString();
            }
        }
        public ParentApplication parentApplication { get; set; } = new ParentApplication();
    }
}
