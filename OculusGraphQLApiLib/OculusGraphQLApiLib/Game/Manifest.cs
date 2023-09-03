using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Game
{
    public class Manifest
    {
        public string appId { get; set; } = "";
        public string canonicalName { get; set; } = "";
        public bool isCore { get; set; } = false;
        public string packageType { get; set; } = "APP";
        public string launchFile { get; set; } = "";
        public string launchParameters { get; set; } = "";
        public string launchFile2D { get; set; } = null;
        public string launchParameters2D { get; set; } = "";
        public string version { get; set; } = "1.0";
        public int versionCode { get; set; } = 0;
        public string[] redistributables { get; set; } = new string[0];
        public Dictionary<string, ManifestFile> files { get; set; } = new Dictionary<string, ManifestFile>();
        public bool firewallExceptionsRequired { get; set; } = false;
        public string parentCanonicalName { get; set; } = null;
        public int manifestVersion { get; set; } = 1;

        public Manifest GetMinimal()
        {
            Manifest mini = this;
            mini.files = new Dictionary<string, ManifestFile>();
            foreach (KeyValuePair<string, ManifestFile> file in this.files)
            {
                if (file.Key.ToLower().EndsWith(".exe"))
                {
                    mini.files.Add(file.Key, file.Value);
                    break;
                }
            }
            return mini;
        }
    }

    public class ManifestFile
    {
        public string sha256 { get; set; } = "";
        public long size { get; set; } = 0;
        public long segmentSize { get; set; } = 10000000;
        public object[][] segments { get; set; } = new object[0][];
    }
}
