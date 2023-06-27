using System.Net;
using System.Text.Json;
using ComputerUtils.Logging;
using OculusDB.ScrapingMaster;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeScraper
{
    public List<ScrapingTask> scrapingTasks { get; set; } = new List<ScrapingTask>();
    public ScrapingNodeTaskResult taskResult { get; set; } = new ScrapingNodeTaskResult();
    public ScrapingNodeManager scrapingNodeManager { get; set; } = new ScrapingNodeManager();

    public ScrapingNodeScraper(ScrapingNodeManager manager)
    {
        scrapingNodeManager = manager;
    }

    public void DoTasks()
    {
        taskResult = new ScrapingNodeTaskResult();
        while (scrapingTasks.Count > 0)
        {
            switch (scrapingTasks[0].scrapingTask)
            {
                case ScrapingTaskType.GetAllAppsToScrape:
                    TransmitAndClearResultsIfPresent();
                    taskResult.scrapingNodeTaskResultType = ScrapingNodeTaskResultType.FoundAppsToScrape;
                    try
                    {
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.HOLLYWOOD));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.RIFT));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.GEARVR));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.PACIFIC));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeForHeadset(Headset.SEACLIFF));
                        taskResult.appsToScrape.AddRange(CollectAppsToScrapeFromApplab());
                    }
                    catch (Exception e)
                    {
                        Logger.Log("Couldn't collect apps to scrape: " + e, LoggingType.Error);
                        Logger.Log("Informing server of error");
                        taskResult.scrapingNodeTaskResultType =
                            ScrapingNodeTaskResultType.ErrorWhileRequestingAppsToScrape;
                    }
                    
                    break;
            }
            // After task is done remove it from the scrapingTasks list
            scrapingTasks.RemoveAt(0);
        }
        
        // After doing all tasks Transmit results if there are any
        TransmitAndClearResultsIfPresent();
    }

    public void TransmitAndClearResultsIfPresent()
    {
        if (!taskResult.altered) return;
        Logger.Log("Transmitting results");
        scrapingNodeManager.GetResponseOfPostRequest("scrapingNode/submitTaskResult", JsonSerializer.Serialize(taskResult));
        taskResult = new ScrapingNodeTaskResult();
    }
    
    public List<AppToScrape> CollectAppsToScrapeForHeadset(Headset h)
    {
        List<AppToScrape> appsToScrape = new List<AppToScrape>();
        int apps = 0;
        Logger.Log("Adding apps to scrape for " + HeadsetTools.GetHeadsetCodeName(h));
        try
        {
            foreach (Application a in OculusInteractor.EnumerateAllApplications(h))
            {
                apps++;
                appsToScrape.Add(new AppToScrape { headset = h, appId = a.id, priority = false, imageUrl = a.cover_square_image.uri });
            }
        } catch(Exception e)
        {
            Logger.Log(e.ToString(), LoggingType.Warning);
        }
        Logger.Log("Found " + apps + " apps to scrape for " + HeadsetTools.GetHeadsetCodeName(h));
        return appsToScrape;
    }
    
    public List<AppToScrape> CollectAppsToScrapeFromApplab()
    {
        List<AppToScrape> appsToScrape = new List<AppToScrape>();
        WebClient c = new WebClient();
        int lastCount = -1;
        bool didIncrease = true;
        List<SidequestApplabGame> s = new List<SidequestApplabGame>();
        while(didIncrease)
        {
            s.AddRange(JsonSerializer.Deserialize<List<SidequestApplabGame>>(c.DownloadString("https://api.sidequestvr.com/v2/apps?limit=1000&skip=" + s.Count + "&is_app_lab=true&has_oculus_url=true&sortOn=downloads&descending=true")));
            didIncrease = lastCount != s.Count;
            lastCount = s.Count;
        }
        Logger.Log("queued " + lastCount + " applab apps");
        foreach (SidequestApplabGame a in s)
        {
            string id = a.oculus_url.Replace("/?utm_source=sidequest", "").Replace("?utm_source=sq_pdp&utm_medium=sq_pdp&utm_campaign=sq_pdp&channel=sq_pdp", "").Replace("https://www.oculus.com/experiences/quest/", "").Replace("/", "");
            if (id.Length <= 16)
            {
                appsToScrape.Add(new AppToScrape { appId = id, imageUrl = a.image_url, priority = false, headset = Headset.HOLLYWOOD });
            }
        }

        return appsToScrape;
    }
}