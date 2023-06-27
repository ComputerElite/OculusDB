using System.Text.Json;
using ComputerUtils.Webserver;

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
        ScrapingNodeMongoDBManager.Init();
        MongoDBInteractor.Initialize();
        server = httpServer;
        server.AddRoute("POST", "/api/v1/gettasks", request =>
        {
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
        server.AddRoute("POST", "/api/v1/taskresults", request =>
        {
            ScrapingNodeIdentification scrapingNodeIdentification = JsonSerializer.Deserialize<ScrapingNodeIdentification>(request.bodyString);
            ScrapingNodeAuthenticationResult r = ScrapingNodeMongoDBManager.CheckScrapingNode(scrapingNodeIdentification);
            if (!r.tokenAuthorized)
            {
                request.SendString(JsonSerializer.Serialize(r), "application/json", 403);
                return true;
            }
            // Process results of scraping:
            //   - When apps for scraping have been sent, add them to the DB for scraping
            //   - When scraping is done, compute the activity entries and write both to the DB (Each scraped app should)
            
            return true;
        });
    }
}