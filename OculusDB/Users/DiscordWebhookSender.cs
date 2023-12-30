using ComputerUtils.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusDB.ScrapingMaster;

namespace OculusDB.Users
{
    public class DiscordWebhookSender
    {
        public static List<DateTime> threadStartTimes = new List<DateTime>();
        public static void StartFlushDifferenceThread()
        {
            Thread t = new Thread(() =>
            {
                threadStartTimes.Add(DateTime.UtcNow);
                if (threadStartTimes.Count > 5)
                {
                    if (DateTime.UtcNow - threadStartTimes[0] < TimeSpan.FromMinutes(10))
                    {
                        ScrapingMasterServer.SendMasterWebhookMessage("Flush difference thread crashed too often",  "Flush difference thread crashed at least 5 times during the last 10 minutes. I will NOT restart it anymore.", 0xFF0000);
                        return;
                    }
                    threadStartTimes.RemoveAt(0);
                }
                try
                {
                    while (true)
                    {
                        List<DifferenceWebhook> differenceWebhooks = GetWebhooks();
                        if (differenceWebhooks.Count <= 0)
                        {
                            // no webhooks, sleep for 10 seconds
                            Logger.Log("No webhooks to process for");
                            Thread.Sleep(10000);
                        }
                        List<DBDifference> diffs = OculusDBDatabase.GetDiffsFromQueue(100);
                        if (diffs.Count <= 0)
                        {
                            // no diffs to process, wait 10 seconds
                            Logger.Log("No diff to process");
                            Thread.Sleep(10000);
                        }
                        
                        foreach (DBDifference diff in diffs)
                        {
                            Logger.Log("Processing " + diff.__id);
                            foreach (DifferenceWebhook differenceWebhook in differenceWebhooks)
                            {
                                try
                                {
                                    switch(differenceWebhook.type)
                                    {
                                        case DifferenceWebhookType.Discord:
                                            differenceWebhook.SendDiscordWebhook(diff);
                                            break;
                                        case DifferenceWebhookType.OculusDB:
                                            differenceWebhook.SendOculusDbWebhook(diff);
                                            break;
                                    }
                                    
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log("Couldn't send webhook: " + ex.ToString(), LoggingType.Error);
                                    break;
                                }
                            }
                            OculusDBDatabase.SetDiffProcessed(diff);
                        }
                        
                    }
                }
                catch (Exception e)
                {
                    ScrapingError error = ScrapingNodeMongoDBManager.AddErrorReport(new ScrapingError
                    {
                        errorMessage = e.ToString(),
                        scrapingNodeId = "MASTER-SERVER"
                    }, new ScrapingNodeAuthenticationResult
                    {
                        scrapingNode = new ScrapingNode
                        {
                            scrapingNodeId = "MASTER-SERVER"
                        }
                    });
                    ScrapingMasterServer.SendMasterWebhookMessage("Flush difference thread crashed!",  "Flush difference thread crashed. I'll attempt to restart it.\n\n" + OculusDBEnvironment.config.scrapingMasterUrl + "/api/v1/scrapingerror/" + error.__id, 0xFFFF00);
                    StartFlushDifferenceThread();
                }
                
            });
            t.Start();
        }

        public static List<DifferenceWebhook> webhooks = new();
        public static DateTime lastUpdatedWebhooks = DateTime.MinValue;
        public static List<DifferenceWebhook> GetWebhooks()
        {
            if(DateTime.UtcNow - lastUpdatedWebhooks > TimeSpan.FromMinutes(5))
            {
                webhooks = OculusDBDatabase.GetAllWebhooks();
                lastUpdatedWebhooks = DateTime.UtcNow;
            }

            return webhooks;
        }
    }
}
