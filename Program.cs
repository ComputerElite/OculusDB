using ComputerUtils.CommandLine;
using ComputerUtils.Logging;
using ComputerUtils.RandomExtensions;
using ComputerUtils.Updating;
using ComputerUtils.Webserver;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace OculusDB
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.displayLogInConsole = true;
            CommandLineCommandContainer cla = new CommandLineCommandContainer(args);
            cla.AddCommandLineArgument(new List<string> { "--workingdir" }, false, "Sets the working Directory for OculusDB", "directory", "");
            cla.AddCommandLineArgument(new List<string> { "update", "--update", "-U" }, true, "Starts in update mode (use with caution. It's best to let it do on it's own)");
            //cla.AddCommandLineArgument(new List<string> { "--displayMasterToken", "-dmt" }, true, "Outputs the master token without starting the server");
            if (cla.HasArgument("help"))
            {
                cla.ShowHelp();
                return;
            }

            string workingDir = cla.GetValue("--workingdir");
            if (workingDir.EndsWith("\"")) workingDir = workingDir.Substring(0, workingDir.Length - 1);

            OculusDBEnvironment.workingDir = workingDir;
            OculusDBEnvironment.AddVariablesDependentOnVariablesAndFixAllOtherVariables();
            if (cla.HasArgument("update"))
            {
                Updater.UpdateNetApp(Path.GetFileName(Assembly.GetExecutingAssembly().Location), OculusDBEnvironment.workingDir);
            }
            OculusDBEnvironment.config = Config.LoadConfig();
            if (OculusDBEnvironment.config.masterToken == "") OculusDBEnvironment.config.masterToken = RandomExtension.CreateToken();
            OculusDBEnvironment.config.Save();
            //Logger.SetLogFile(workingDir + "Log.log");

            

            OculusDBServer s = new OculusDBServer();
            HttpServer server = new HttpServer();
            s.StartServer(server);
        }
    }
}