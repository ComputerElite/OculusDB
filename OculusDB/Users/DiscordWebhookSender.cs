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
        public static void StartFlushDifferenceThread()
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    List<DifferenceWebhook> differenceWebhooks = GetWebhooks();
                    if (differenceWebhooks.Count <= 0)
                    {
                        // no webhooks, sleep for 10 seconds
                        Thread.Sleep(10000);
                        continue;
                    }
                    List<DBDifference> diffs = OculusDBDatabase.GetDiffsFromQueue(100);
                    if (diffs.Count <= 0)
                    {
                        // no diffs to process, wait 10 seconds
                        Thread.Sleep(10000);
                    }
                    
                    foreach (DBDifference diff in diffs)
                    {
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
