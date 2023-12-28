using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OculusDB.MongoDB;

namespace OculusDB.Database
{
    public class DLCLists
    {
        public List<DBIapItem> iapItems { get; set; } = new List<DBIapItem>();
        public List<DBIapItemPack> iapItemPacks { get; set; } = new List<DBIapItemPack>();

        public void PopulateAll(PopulationContext context)
        {
            iapItems.ForEach(x => x.PopulateSelf(context));
            iapItemPacks.ForEach(x => x.PopulateSelf(context));
        }
    }
}
