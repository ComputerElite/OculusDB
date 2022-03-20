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
