using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ComputerUtils.Logging;
using OculusDB.ScrapingMaster;

namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeManager
{
    public ScrapingNodeScraper scraper;
    public ScrapingNodeStatus status { get; set; } = ScrapingNodeStatus.StartingUp;
    public ScrapingNodeConfig config = new ScrapingNodeConfig();
    public ScrapingNode scrapingNode = new ScrapingNode();
    HttpClient client = new HttpClient();
    
    public void StartNode(ScrapingNodeConfig c)
    {
        scraper = new ScrapingNodeScraper(this);
        client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OculusDB", OculusDBEnvironment.updater.version));
        if (!CheckOutServer())
        {
            Logger.Log("Master server reports something invalid about the scraping node. Refer to above message for more info. Scraping node will NOT start.", LoggingType.Error);
            return;
        }
        Logger.Log("Initializing Oculus Interactor");
        OculusInteractor.Init();
        config = c;
        GetScrapingTasks();
        scraper.DoTasks();
    }

    /// <summary>
    /// Check with the server to see if the node is allowed to scrape and set Node info with server info
    /// </summary>
    /// <returns>Successful authentication with server and version cross check</returns>
    private bool CheckOutServer()
    {
        Logger.Log("Checking out master server");
        string json = GetResponseOfPostRequest(config.masterAddress + "/api/v1/authenticate", JsonSerializer.Serialize(GetIdentification()));
        ScrapingNodeAuthenticationResult res = JsonSerializer.Deserialize<ScrapingNodeAuthenticationResult>(json);
        Logger.Log("Response from master server: " + res.msg, res.tokenAuthorized ? LoggingType.Info : LoggingType.Error);
        return res.tokenAuthorized;
    }

    public void GetScrapingTasks()
    {
        Logger.Log("Requesting scraping tasks");
        status = ScrapingNodeStatus.RequestingToDo;
        string json = GetResponseOfPostRequest(config.masterAddress + "/api/v1/gettasks",
            JsonSerializer.Serialize(GetIdentification()));
        scraper.scrapingTasks.AddRange(JsonSerializer.Deserialize<List<ScrapingTask>>(json));
    }

    public string GetResponseOfPostRequest(string url, string body)
    {
        return SendPostRequest(url, body).Content.ReadAsStringAsync().Result;
    }
    
    public HttpResponseMessage SendPostRequest(string url, string body)
    {
        return client.Send(ConstructPostRequest(url, body));
    }

    public ScrapingNodeIdentification GetIdentification()
    {
        return new ScrapingNodeIdentification
        {
            scrapingNodeToken = config.scrapingNodeToken,
            scrapingNodeVersion = OculusDBEnvironment.updater.version,
            tokenCount = config.oculusTokens.Count
        };
    }

    public HttpRequestMessage ConstructPostRequest(string url, string body)
    {
        return new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            Content = new StringContent(body),
            RequestUri = new Uri(url)
        };
    }
}