using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using ComputerUtils.Logging;
using ComputerUtils.Updating;
using OculusDB.ScrapingMaster;
using OculusGraphQLApiLib;

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
            bool sh = _status != value;
            _status = value;
            if(sh) scraper.SendHeartBeat();
        }
    }

    public ScrapingNodeConfig config = new ScrapingNodeConfig();
    public ScrapingNode scrapingNode = new ScrapingNode();
    public bool nodeRunning = true;
    HttpClient client = new HttpClient();
    
    public void StartNode(ScrapingNodeConfig c)
    {
        config = c;
        if (c.masterAddress == "")
        {
            Logger.Log("No master server address set. Use 'dotnet OculusDB.dll --sm <server url>' to set it.", LoggingType.Error);
            return;
        }
        if (c.oculusTokens.Count <= 0)
        {
            Logger.Log("No oculus token set. Use 'dotnet OculusDB.dll --so <oculus token>' to set it.", LoggingType.Error);
            return;
        }
        if (c.scrapingNodeToken == "")
        {
            Logger.Log("No scraping node token set. Use 'dotnet OculusDB.dll --st <scraping node token>' to set it.", LoggingType.Error);
            return;
        }
        OculusInteractor.Init();
        scraper = new ScrapingNodeScraper(this);
        client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(20);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("OculusDB", OculusDBEnvironment.updater.version));
        // Check out server to let it know the node is stared. If errors occur during the checkout, the node will do the appropriate action automatically
        ScrapingNodeAuthenticationResult r = CheckOutServer();
        if (r.overrideSettings.overrideCurrency != "")
        {
            config.overrideCurrency = r.overrideSettings.overrideCurrency;
            config.Save();
        }
        Logger.Log("Initializing Oculus Interactor");
        scraper.ChangeToken();
        
        // Start heartbeat loop
        Thread heartBeat = new Thread(() => scraper.HeartBeatLoop());
        heartBeat.Start();
        if (config.doForceScrape)
        {
            scraper.scrapingTasks.Add(new ScrapingTask
            {
                scrapingTask = ScrapingTaskType.ScrapeApp,
                appToScrape = new AppToScrape
                {
                    headset = Headset.MONTEREY,
                    appId = config.appId,
                    priority = config.isPriorityScrape,
                    imageUrl = ""
                }
            });
            scraper.DoTasks();
            nodeRunning = false;
            Environment.Exit(0);
            return;
        }
        while (true)
        {
            GetScrapingTasks();
            scraper.DoTasks();
        }
    }

    private void CheckAuthorizationResponse(ScrapingNodeAuthenticationResult r)
    {
        if (!r.scrapingNodeVersionCompatible && r.compatibleScrapingVersion != "")
        {
            Logger.Log("Version of scraping node is outdated. This version: " + OculusDBEnvironment.updater.version + ", Server version: " + r.compatibleScrapingVersion, LoggingType.Error);
            UpdateScrapingNode();
        }

        if (r.tokenExpired)
        {
            Logger.Log("Your token expired. Contact ComputerElite to get it renewed.", LoggingType.Error);
            Environment.Exit(1);
        }

        if (!r.tokenValid)
        {
            Logger.Log("Your token is not valid. Contact ComputerElite to get a token", LoggingType.Error);
            Environment.Exit(1);
        }
        Logger.Log("An unknown error occured, however it's most likely temporary and thus we'll continue as normal", LoggingType.Warning);
    }

    private bool nodeUpdating = false;
    public void UpdateScrapingNode()
    {
        Logger.Log("Trying to update node");
        if (nodeUpdating)
        {
            Logger.Log("Node is already updating via another thread. Not starting another update attempt.", LoggingType.Warning);
            return;
        }
        try
        {
            WebClient c = new WebClient();
            byte[] updateFile = c.DownloadData(config.masterAddress + "/cdn/node.zip?time=" + DateTime.UtcNow.Ticks);
            Updater.StartUpdateNetApp(updateFile, Path.GetFileName(Assembly.GetExecutingAssembly().Location), OculusDBEnvironment.workingDir);
        }
        catch (Exception e)
        {
            Logger.Log("Couldn't update node automatically. Please update it manually by replacing all files with their new version.", LoggingType.Error);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Check with the server to see if the node is allowed to scrape and set Node info with server info
    /// </summary>
    /// <returns>Successful authentication with server and version cross check</returns>
    private ScrapingNodeAuthenticationResult CheckOutServer()
    {
        Logger.Log("Checking out master server via " + config.masterAddress + "/api/v1/authenticate");
        string json;
        ScrapingNodeAuthenticationResult res;
        try
        {
            ScrapingNodePostResponse r = GetResponseOfPostRequest(config.masterAddress + "/api/v1/authenticate",
                JsonSerializer.Serialize(GetIdentification()));
            if (r.status == 200)
            {
                json = r.json;
                res = JsonSerializer.Deserialize<ScrapingNodeAuthenticationResult>(json);
            }
            else throw new Exception("Server error");
        }
        catch (Exception e)
        {
            res = new ScrapingNodeAuthenticationResult
            {
                msg = "Server sent invalid response",
                tokenAuthorized = false,
                tokenExpired = false,
                tokenValid = false,
                compatibleScrapingVersion = OculusDBEnvironment.updater.version,
                scrapingNode = new ScrapingNode
                {
                    scrapingNodeVersion = OculusDBEnvironment.updater.version
                }
            };
        }
        Logger.Log("Response from master server: " + res.msg, res.tokenAuthorized ? LoggingType.Info : LoggingType.Error);
        return res;
    }

    public void GetScrapingTasks()
    {
        Logger.Log("Requesting scraping tasks");
        if(status != ScrapingNodeStatus.WaitingForMasterServer) status = ScrapingNodeStatus.RequestingToDo;
        try
        {
            string json = GetResponseOfPostRequest(config.masterAddress + "/api/v1/gettasks",
                JsonSerializer.Serialize(GetIdentification())).json;
            scraper.scrapingTasks.AddRange(JsonSerializer.Deserialize<List<ScrapingTask>>(json));
        }
        catch (Exception e)
        {
            Logger.Log("Error while requesting scraping tasks. Server might not be reachable. Retrying in 1 minute: " + e.Message, LoggingType.Error);
            Thread.Sleep(1000 * 60);
        }
    }

    public ScrapingNodePostResponse GetResponseOfPostRequest(string url, string body)
    {
        ScrapingNodePostResponse r = new ScrapingNodePostResponse();
        HttpResponseMessage m = SendPostRequest(url, body);
        r.json = m.Content.ReadAsStringAsync().Result;
        r.status = (int)m.StatusCode;
        if (r.status != 200)
        {
            Logger.Log("Server responded with status code " + r.status, LoggingType.Error);
            if (r.status == 403)
            {
                Logger.Log("Status code is 403, checking response", LoggingType.Warning);
                ScrapingNodeAuthenticationResult a = JsonSerializer.Deserialize<ScrapingNodeAuthenticationResult>(r.json);
                CheckAuthorizationResponse(a);
            }
        }
        return r;
    }
    
    public HttpResponseMessage SendPostRequest(string url, string body)
    {
        HttpResponseMessage m = client.SendAsync(ConstructPostRequest(url, body)).Result;
        if (!m.IsSuccessStatusCode)
        {
            
        }
        return m;
    }

    public ScrapingNodeIdentification GetIdentification()
    {
        return new ScrapingNodeIdentification
        {
            scrapingNodeToken = config.scrapingNodeToken,
            scrapingNodeVersion = OculusDBEnvironment.updater.version,
            tokenCount = config.oculusTokens.Count,
            currency = scraper.GetCurrency()
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