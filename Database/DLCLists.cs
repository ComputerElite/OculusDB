using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DLCLists
    {
        public List<DBIAPItem> dlcs { get; set; } = new List<DBIAPItem>();
        public List<DBIAPItemPack> dlcPacks { get; set; } = new List<DBIAPItemPack>();
    }
}
