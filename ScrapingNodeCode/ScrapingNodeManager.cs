using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ComputerUtils.Logging;
using OculusDB.ScrapingMaster;

namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeManager
{
    public ScrapingNodeScraper scraper;
    private ScrapingNodeStatus _status = ScrapingNodeStatus.StartingUp;

    public ScrapingNodeStatus status
    {
        get => _status;
        set
        {
            if(_status != status) scraper.SendHeartBeat();
            _status = value;
        }
    }

    public ScrapingNodeConfig config = new ScrapingNodeConfig();
    public ScrapingNode scrapingNode = new ScrapingNode();
    HttpClient client = new HttpClient();
    
    public void StartNode(ScrapingNodeConfig c)
    {
        config = c;
        scraper = new ScrapingNodeScraper(this);
        client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(20);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OculusDB", OculusDBEnvironment.updater.version));
        if (!CheckOutServer())
        {
            Logger.Log("Master server reports something invalid about the scraping node. Refer to above message for more info. Scraping node will NOT start.", LoggingType.Error);
            return;
        }
        Logger.Log("Initializing Oculus Interactor");
        OculusInteractor.Init();
        scraper.ChangeToken();
        
        // Start heartbeat loop
        Thread heartBeat = new Thread(() => scraper.HeartBeatLoop());
        heartBeat.Start();
        while (true)
        {
            GetScrapingTasks();
            scraper.DoTasks();
        }
    }

    /// <summary>
    /// Check with the server to see if the node is allowed to scrape and set Node info with server info
    /// </summary>
    /// <returns>Successful authentication with server and version cross check</returns>
    private bool CheckOutServer()
    {
        Logger.Log("Checking out master server via " + config.masterAddress + "/api/v1/authenticate");
        string json;
        ScrapingNodeAuthenticationResult res;
        try
        {
            json = GetResponseOfPostRequest(config.masterAddress + "/api/v1/authenticate", JsonSerializer.Serialize(GetIdentification()));
            res = JsonSerializer.Deserialize<ScrapingNodeAuthenticationResult>(json);
        }
        catch (Exception e)
        {
            res = new ScrapingNodeAuthenticationResult
            {
                msg = "Server sent invalid response",
                tokenAuthorized = false,
                tokenExpired = false,
                tokenValid = false,
            };
        }
        Logger.Log("Response from master server: " + res.msg, res.tokenAuthorized ? LoggingType.Info : LoggingType.Error);
        return res.tokenAuthorized;
    }

    public void GetScrapingTasks()
    {
        Logger.Log("Requesting scraping tasks");
        status = ScrapingNodeStatus.RequestingToDo;
        try
        {
            string json = GetResponseOfPostRequest(config.masterAddress + "/api/v1/gettasks",
                JsonSerializer.Serialize(GetIdentification()));
            scraper.scrapingTasks.AddRange(JsonSerializer.Deserialize<List<ScrapingTask>>(json));
        }
        catch (Exception e)
        {
            Logger.Log("Error while requesting scraping tasks. Server might not be reachable. Retrying in 1 minute: " + e.Message, LoggingType.Error);
            Thread.Sleep(1000 * 60);
        }
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