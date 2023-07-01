using System.Text.Json;
using ComputerUtils.Logging;
using ComputerUtils.Webserver;
using OculusDB.Database;
using OculusDB.ScrapingNodeCode;

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
            if (r.tokenAuthorized)
            {
                ScrapingManaging.OnNodeStarted(r);
            }
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


            ScrapingManaging.ProcessHeartBeat(heartBeat, r);
            return true;
        });
        server.AddRoute("GET", "/api/v1/scrapingnodes", request =>
        {
            request.SendString(JsonSerializer.Serialize(ScrapingNodeMongoDBManager.GetScrapingNodes()), "application/json");
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
        server.AddRouteFile("/style.css", frontend + "style.css", FrontendServer.replace, true, true, true, 0, true);
        server.AddRouteFile("/", frontend + "scrapingMaster.html", FrontendServer.replace, true, true, true, 0, true);

        server.StartServer(config.port);
    }
}