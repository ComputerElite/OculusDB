using System.IO.Compression;
using System.Text.Json;
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

            request.SendString(JsonSerializer.Serialize(ScrapingManaging.ProcessTaskResult(taskResult, r)), "application/json");
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
            request.SendString(JsonSerializer.Serialize(ScrapingNodeMongoDBManager.GetScrapingNodes().ConvertAll(x =>
            {
                x.SetOnline();
                return x;
            })), "application/json");
            return true;
        });
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

        
        server.StartServer(config.port);
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
            if (relativePath.Contains("data") || relativePath.Contains("frontend")) return;
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