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
        public List<DBIAPItem> iapItems { get; set; } = new List<DBIAPItem>();
        public List<DBIAPItemPack> iapItemPacks { get; set; } = new List<DBIAPItemPack>();

        public void PopulateAll(PopulationContext context)
        {
            iapItems.ForEach(x => x.PopulateSelf(context));
            iapItemPacks.ForEach(x => x.PopulateSelf(context));
        }
    }
}
