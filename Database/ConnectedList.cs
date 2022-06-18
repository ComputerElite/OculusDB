using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class ConnectedList : DLCLists
    {
        public List<DBApplication> applications { get; set; } = new List<DBApplication>();
        public List<DBVersion> versions { get; set; } = new List<DBVersion>();
    }
}
