using ComputerUtils.FileManaging;
using ComputerUtils.RandomExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB
{
    public class OculusDBEnvironment
    {
        public static string workingDir = "";
        public static string dataDir = "";
        public static Config config = new Config();
        public static string userAgent { get
            {
                return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.64 Safari/537.36 Edg/101.0.1210.47";
            } }

        public static void AddVariablesDependentOnVariablesAndFixAllOtherVariables()
        {
            if (!workingDir.EndsWith(Path.DirectorySeparatorChar)) workingDir += Path.DirectorySeparatorChar;
            if (workingDir == Path.DirectorySeparatorChar.ToString()) workingDir = AppDomain.CurrentDomain.BaseDirectory;
            dataDir = workingDir + "data" + Path.DirectorySeparatorChar;
            FileManager.CreateDirectoryIfNotExisting(workingDir);
            FileManager.CreateDirectoryIfNotExisting(dataDir);
        }
    }
}
