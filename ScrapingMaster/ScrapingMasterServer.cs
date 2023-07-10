using System.IO.Compression;
using System.Text.Json;
using ComputerUtils.Discord;
using ComputerUtils.Logging;
using ComputerUtils.Webserver;
using Ionic.Zip;
using OculusDB.Database;
using OculusDB.ScrapingNodeCode;
using ZipFile = System.IO.Compression.ZipFile;

namespace OculusDB.ScrapingMaster;

public class ScrapingMasterServer
{
    public HttpServer server;
    public Config config
    {
        get { return OculusDBEnvironment.config; }
    }

    public void StartServer(HttpServer httpServer)
    {
        bool createdNodeZip = false;
        bool creatingNodeZip = false;
        string frontend = OculusDBEnvironment.debugging ? @"..\..\..\frontend\" : "frontend" + Path.DirectorySeparatorChar;
        ScrapingNodeMongoDBManager.Init();
        MongoDBInteractor.Initialize();
        
        ScrapingNodeMongoDBManager.CheckActivityCollection();
        Thread nodeStatusThread = new Thread(() =>
        {
            MonitorNodes();
        });
        nodeStatusThread.Start();
        server = httpServer;
        server.AddRoute("POST", "/api/v1/gettasks", request =>
        {
            // Check if the scraping node is authorized to scrape
            ScrapingNodeIdentification scrapingNodeIdentification = JsonSerializer.Deserialize<ScrapingNodeIdentification>(request.bodyString);
            ScrapingNodeAuthenticationResult r = ScrapingNodeMongoDBManager.CheckScrapingNode(scrapingNodeIdentification);
            if (!r.tokenAuthorized)
            {
                request.SendString(JsonSerializer.Serialize(r), "application/json", 403);
                return true;
            }
            request.SendString(JsonSerializer.Serialize(ScrapingManaging.GetTasks(r)), "application/json");
            return true;
        });
        server.AddRoute("POST", "/api/v1/authenticate", request =>
        {
            // Authenticate the scraping node and send back the scraping node info
            ScrapingNodeIdentification scrapingNodeIdentification = JsonSerializer.Deserialize<ScrapingNodeIdentification>(request.bodyString);
            ScrapingNodeAuthenticationResult r = ScrapingNodeMongoDBManager.CheckScrapingNode(scrapingNodeIdentification);
            if (!r.tokenAuthorized)
            {
                request.SendString(JsonSerializer.Serialize(r), "application/json", 403);
                return true;
            }
            ScrapingManaging.OnNodeStarted(r);
            request.SendString(JsonSerializer.Serialize(r), "application/json");
            return true;
        });
        server.AddRoute("POST", "/api/v1/taskresults", request =>
        {
            // Check if the scraping node is authorized to scrape
            ScrapingNodeTaskResult taskResult = JsonSerializer.Deserialize<ScrapingNodeTaskResult>(request.bodyString);
            ScrapingNodeAuthenticationResult r = ScrapingNodeMongoDBManager.CheckScrapingNode(taskResult.identification);
            if (!r.tokenAuthorized)
            {
                request.SendString(JsonSerializer.Serialize(r), "application/json", 403);
                return true;
            }

            request.SendString(JsonSerializer.Serialize(new ScrapingNodeTaskResultProcessing()), "application/json");
            ScrapingManaging.ProcessTaskResult(taskResult, r);
            return true;
        });
        server.AddRoute("POST", "/api/v1/heartbeat", request =>
        {
            // Check if the scraping node is authorized to scrape
            ScrapingNodeHeartBeat heartBeat = JsonSerializer.Deserialize<ScrapingNodeHeartBeat>(request.bodyString);
            ScrapingNodeAuthenticationResult r = ScrapingNodeMongoDBManager.CheckScrapingNode(heartBeat.identification);
            if (!r.tokenAuthorized)
            {
                request.SendString(JsonSerializer.Serialize(r), "application/json", 403);
                return true;
            }


            request.SendString(JsonSerializer.Serialize(ScrapingManaging.ProcessHeartBeat(heartBeat, r)), "application/json");
            return true;
        });
        server.AddRoute("GET", "/api/v1/scrapingnodes", request =>
        {
            request.SendString(JsonSerializer.Serialize(ScrapingNodeMongoDBManager.GetScrapingNodes()), "application/json");
            return true;
        }, false, true, true, true, 4); // 4 seconds in cache
        server.AddRoute("GET", "/api/v1/processingstats", request =>
        {
            request.SendString(JsonSerializer.Serialize(ScrapingNodeMongoDBManager.GetScrapingProcessingStats()), "application/json");
            return true;
        }, false, true, true, true, 4);
        server.AddRoute("POST", "/api/v1/versions/", new Func<ServerRequest, bool>(request =>
        {
            ScrapingNodeIdentification scrapingNodeIdentification = JsonSerializer.Deserialize<ScrapingNodeIdentification>(request.bodyString);
            ScrapingNodeAuthenticationResult r = ScrapingNodeMongoDBManager.CheckScrapingNode(scrapingNodeIdentification);
            if (!r.tokenAuthorized)
            {
                request.SendString(JsonSerializer.Serialize(r), "application/json", 403);
                return true;
            }
            try
            {
                List<DBVersion> versions = MongoDBInteractor.GetVersions(request.pathDiff, request.queryString.Get("onlydownloadable") != null && request.queryString.Get("onlydownloadable").ToLower() != "false");
                request.SendString(JsonSerializer.Serialize(versions), "application/json");
            }
            catch (Exception e)
            {
                request.SendString("An unknown error occurred", "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }), true, true, true, true, 360); // 6 mins
        server.AddRoute("GET", "/cdn/node.zip", request =>
        {
            if (!createdNodeZip)
            {
                if (!creatingNodeZip)
                {
                    creatingNodeZip = true;
                    CreateNodeZip();
                    createdNodeZip = true;
                    creatingNodeZip = false;
                }
                else
                {
                    while (creatingNodeZip)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            request.SendFile(OculusDBEnvironment.dataDir + "node.zip");
            return true;
        });
        server.AddRouteFile("/style.css", frontend + "style.css", FrontendServer.replace, true, true, true, 0, true);
        server.AddRouteFile("/", frontend + "scrapingMaster.html", FrontendServer.replace, true, true, true, 0, true);
        server.AddRouteFile("/setup", frontend + "setupNode.html", FrontendServer.replace, true, true, true, 0, true);
        server.AddRouteFile("/logo", frontend + "logo.png", true, true, true);

        server.StartServer(config.port);
    }

    public void SendMasterWebhookMessage(string title, string description, int color)
    {
        if (config.nodeStatusWebhookUrl == "") return;
        try
        {
            Logger.Log("Sending master webhook");
            DiscordWebhook webhook = new DiscordWebhook(config.nodeStatusWebhookUrl);
            webhook.SendEmbed(title, description, "master " + DateTime.UtcNow + " UTC", "OculusDB", config.scrapingMasterUrl + "logo", config.scrapingMasterUrl, config.scrapingMasterUrl + "logo", config.scrapingMasterUrl, color);
        }
        catch (Exception ex)
        {
            Logger.Log("Exception while sending webhook" + ex.ToString(), LoggingType.Warning);
        }
    }
    
    private void MonitorNodes()
    {
        Dictionary<string, bool> wasOnline = new Dictionary<string, bool>();
        while (true)
        {
            List<ScrapingNodeStats> nodes = ScrapingNodeMongoDBManager.GetScrapingNodes();
            foreach (ScrapingNodeStats node in nodes)
            {
                if (!wasOnline.ContainsKey(node.scrapingNode.scrapingNodeId))
                {
                    wasOnline.Add(node.scrapingNode.scrapingNodeId, node.online);
                }
                //Logger.Log("Node " + node.scrapingNode.scrapingNodeId + " is " + (node.online ? "online" : "offline"), LoggingType.Debug);

                if (wasOnline[node.scrapingNode.scrapingNodeId] != node.online)
                {
                    //Logger.Log("That is a change", LoggingType.Debug);
                    // Node status changed, send webhook msg
                    SendMasterWebhookMessage("Scraping Node " + node.scrapingNode.scrapingNodeId + " " + (node.online ? "online" : "offline"), "", node.online ? 0x00FF00 : 0xFF0000);
                }
                wasOnline[node.scrapingNode.scrapingNodeId] = node.online;
            }
            Thread.Sleep(15000);
        }
    }

    public void CreateNodeZip()
    {
        string nodeLoc = OculusDBEnvironment.dataDir + "node.zip";
        Logger.Log("Creating Node zip file");
        if(File.Exists(nodeLoc)) File.Delete(nodeLoc);
        CreateZipArchive(AppDomain.CurrentDomain.BaseDirectory, nodeLoc);
    }
    
    public static void CreateZipArchive(string inputDir, string outputZip)
    {
        string rootDirectory = Path.GetDirectoryName(inputDir);
        using (FileStream zipToCreate = new FileStream(outputZip, FileMode.Create))
        {
            using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
            {
                AddFilesToZip(archive, inputDir, rootDirectory);
            }
        }
    }

    private static void AddFilesToZip(ZipArchive archive, string inputDir, string rootDirectory)
    {
        foreach (string filePath in Directory.GetFiles(inputDir))
        {
            string relativePath = filePath.Replace(rootDirectory, "").TrimStart(Path.DirectorySeparatorChar);
            if (relativePath.Contains("data") || relativePath.Contains("frontend/") || relativePath.Contains("updater/") || relativePath.Contains("frontend/") || relativePath.Contains("bin/") || relativePath.Contains(".log") || relativePath.Contains(".zip") || relativePath.Contains("wget-log")) return;
            ZipArchiveEntry entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (Stream entryStream = entry.Open())
                {
                    fileStream.CopyTo(entryStream);
                }
            }
        }

        foreach (string subdirectoryPath in Directory.GetDirectories(inputDir))
        {
            AddFilesToZip(archive, subdirectoryPath, rootDirectory);
        }
    }
}