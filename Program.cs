using ComputerUtils.CommandLine;
using ComputerUtils.Logging;
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

            OculusDBEnvironment.workingDir = workingDir;
            OculusDBEnvironment.AddVariablesDependentOnVariablesAndFixAllOtherVariables();
            OculusDBEnvironment.config = Config.LoadConfig();
            //Logger.SetLogFile(workingDir + "Log.log");

            if (cla.HasArgument("update"))
            {
                Updater.UpdateNetApp(Assembly.GetExecutingAssembly().GetName().FullName, OculusDBEnvironment.workingDir);
            }

            OculusDBServer s = new OculusDBServer();
            HttpServer server = new HttpServer();
            s.StartServer(server);
        }
    }
}