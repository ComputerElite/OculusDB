using ComputerUtils.CommandLine;
using ComputerUtils.Logging;
using ComputerUtils.QR;
using ComputerUtils.RandomExtensions;
using ComputerUtils.Updating;
using ComputerUtils.Webserver;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using OculusDB.ScrapingMaster;
using OculusDB.ScrapingNodeCode;

namespace OculusDB
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.displayLogInConsole = true;
            Logger.saveOutputInVariable = true;
            CommandLineCommandContainer cla = new CommandLineCommandContainer(args);
            cla.AddCommandLineArgument(new List<string> { "--workingdir" }, false, "Sets the working Directory for OculusDB", "directory", "");
            cla.AddCommandLineArgument(new List<string> { "update", "--update", "-U" }, true, "Starts in update mode (use with caution. It's best to let it do on it's own)");
            cla.AddCommandLineArgument(new List<string> { "--displayMasterToken", "--dmt" }, true, "Outputs the master token without starting the server");
            cla.AddCommandLineArgument(new List<string> { "help", "--help" }, true, "Outputs the master token without starting the server");
            cla.AddCommandLineArgument(new List<string> { "--type" }, false, "Sets the OculusDB Server type to 'frontend', 'node' (Scraping Node) or 'master' (Master Scraping node). frontend and master should only be used for hosting an own OculusDB instance.", "Server Type", "node");
            cla.AddCommandLineArgument(new List<string> { "--set-token", "--st" }, false, "Sets the token for the scraping node", "Scraping node token", "");

            
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
            OculusDBEnvironment.scrapingNodeConfig = ScrapingNodeConfig.LoadConfig();
            if (OculusDBEnvironment.config.masterToken == "") OculusDBEnvironment.config.masterToken = RandomExtension.CreateToken();
            OculusDBEnvironment.config.Save();
            //Logger.SetLogFile(workingDir + "Log.log");

            if (cla.HasArgument("-dmt"))
            {
                QRCodeGeneratorWrapper.Display(OculusDBEnvironment.config.masterToken);
                return;
            }

            if (cla.HasArgument("--st"))
            {
                OculusDBEnvironment.scrapingNodeConfig.scrapingNodeToken = cla.GetValue("--st");
                OculusDBEnvironment.scrapingNodeConfig.Save();
            }

            if (cla.HasArgument("--type"))
            {
                switch (cla.GetValue("--type"))
                {
                    case "node":
                        OculusDBEnvironment.config.serverType = OculusDBServerType.ScrapeNode;
                        break;
                    case "frontend":
                        OculusDBEnvironment.config.serverType = OculusDBServerType.Frontend;
                        break;
                    case "master":
                        OculusDBEnvironment.config.serverType = OculusDBServerType.ScrapeMaster;
                        break;
                }
                OculusDBEnvironment.config.Save();
            }

            if (OculusDBEnvironment.config.serverType == OculusDBServerType.Frontend)
            {
                FrontendServer s = new FrontendServer();
                HttpServer server = new HttpServer();
                s.StartServer(server);
            } else if (OculusDBEnvironment.config.serverType == OculusDBServerType.ScrapeMaster)
            {
                ScrapingMasterServer s = new ScrapingMasterServer();
                HttpServer server = new HttpServer();
                s.StartServer(server);
            } else if (OculusDBEnvironment.config.serverType == OculusDBServerType.ScrapeNode)
            {
                // Load scraping config
                ScrapingNodeManager node = new ScrapingNodeManager();
                node.StartNode(OculusDBEnvironment.scrapingNodeConfig);
            }
        }
    }
}