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
            cla.AddCommandLineArgument(new List<string> { "--set-ms", "--sm" }, false, "Set the master scraping server url", "Scraping Master URL", "https://scraping.rui2015.me");
            cla.AddCommandLineArgument(new List<string> { "--set-oculus-token", "--so" }, false, "Sets the Oculus token for the scraping node", "Oculus Token", "");
            cla.AddCommandLineArgument(new List<string> { "--set-currency", "--sc" }, false, "Sets the nodes currency (e. g. if it reports USD but uses AUD)", "currency", "");
            cla.AddCommandLineArgument(new List<string> { "--force-scrape", "--fs" }, false, "Forces a scrape for that app, then quits the node", "App id", "");
            cla.AddCommandLineArgument(new List<string> { "--force-priority", "--fp" }, false, "Forces a priority scrape for that app, then quits the node", "App id", "");

            
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
                Logger.Log("Set scraping node token to " + cla.GetValue("--st"));
                return;
            }
            if (cla.HasArgument("--so"))
            {
                OculusDBEnvironment.scrapingNodeConfig.oculusTokens.Clear();
                OculusDBEnvironment.scrapingNodeConfig.oculusTokens.Add(cla.GetValue("--so"));
                OculusDBEnvironment.scrapingNodeConfig.Save();
                Logger.Log("Set Oculus token to " + cla.GetValue("--so"));
                return;
            }
            if (cla.HasArgument("--sm"))
            {
                OculusDBEnvironment.scrapingNodeConfig.masterAddress = cla.GetValue("--sm");
                OculusDBEnvironment.scrapingNodeConfig.Save();
                Logger.Log("Set master address to " + cla.GetValue("--sm"));
                return;
            }
            if (cla.HasArgument("--sc"))
            {
                OculusDBEnvironment.scrapingNodeConfig.overrideCurrency = cla.GetValue("--sc");
                OculusDBEnvironment.scrapingNodeConfig.Save();
                if (cla.GetValue("--sc") == "")
                {
                    Logger.Log("Currency override disabled");
                }
                else
                {
                    Logger.Log("Set override currency to " + cla.GetValue("--sc"));
                }
                return;
            }

            if (cla.HasArgument("--fp"))
            {
                OculusDBEnvironment.scrapingNodeConfig.doForceScrape = true;
                OculusDBEnvironment.scrapingNodeConfig.appId = cla.GetValue("--fp");
                OculusDBEnvironment.scrapingNodeConfig.isPriorityScrape = true;
            }

            if (cla.HasArgument("--fs"))
            {
                OculusDBEnvironment.scrapingNodeConfig.doForceScrape = true;
                OculusDBEnvironment.scrapingNodeConfig.appId = cla.GetValue("--fs");
                OculusDBEnvironment.scrapingNodeConfig.isPriorityScrape = false;
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
                Logger.Log("Set server type to " + cla.GetValue("--type"));
                return;
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