
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB
{
    public class Update
    {
        public DateTime time { get; set; } = DateTime.Now;
        public string changelog { get; set; } = "";
    }
}
