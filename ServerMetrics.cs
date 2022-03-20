using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB
{
    public class ServerMetrics
    {
        public long ramUsage { get; set; } = 0;
        public string ramUsageString { get; set; } = "";
        public string workingDirectory { get; set; } = "";
    }
}
