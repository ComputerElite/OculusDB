﻿using ComputerUtils.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OculusDB.ObjectConverters;
using OculusDB.ScrapingMaster;

namespace OculusDB.Users
{
    public class DiscordWebhookSender
    {
        public static void SendActivity(DateTime start)
        {
            Logger.Log("Sending activity via Discord webhooks after " + start);
            Thread t = new Thread(() =>
            {
                List<ActivityWebhook> activityWebhooks = MongoDBInteractor.GetWebhooks();
                if (activityWebhooks.Count <= 0) return;
                List<DBDifference> diffs = new List<DBDifference>(); // ToDo: Get all diffs after start
                foreach (ActivityWebhook activityWebhook in activityWebhooks)
                {
                    foreach (DBDifference diff in diffs)
                    {
                        try
                        {
                            switch(activityWebhook.type)
                            {
                                case ActivityWebhookType.Discord:
                                    activityWebhook.SendDiscordWebhook(diff);
                                    break;
                                case ActivityWebhookType.OculusDB:
                                    activityWebhook.SendOculusDBWebhook(diff);
                                    break;
                            }
                            
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Couldn't send webhook: " + ex.ToString(), LoggingType.Error);
                            break;
                        }
                    }
                }
            });
            t.Start();
        }

        public static void SendActivity(List<DBDifference> diffs)
        {
            Logger.Log("Sending " + diffs.Count + " activities via Discord webhooks");
            Thread t = new Thread(() =>
            {
                List<ActivityWebhook> activityWebhooks = MongoDBInteractor.GetWebhooks();
                if (activityWebhooks.Count <= 0) return;
                foreach (ActivityWebhook activityWebhook in activityWebhooks)
                {
                    foreach (DBDifference activity in diffs)
                    {
                        try
                        {
                            switch (activityWebhook.type)
                            {
                                case ActivityWebhookType.Discord:
                                    activityWebhook.SendDiscordWebhook(activity);
                                    break;
                                case ActivityWebhookType.OculusDB:
                                    activityWebhook.SendOculusDBWebhook(activity);
                                    break;
                            }

                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Couldn't send webhook: " + ex.ToString(), LoggingType.Error);
                            break;
                        }
                    }
                }
            });
            t.Start();
        }

        public static List<ActivityWebhook> webhooks = new();
        public static DateTime lastUpdatedWebhooks = DateTime.MinValue;
        public static List<ActivityWebhook> GetWebhooks()
        {
            if(DateTime.UtcNow - lastUpdatedWebhooks > TimeSpan.FromMinutes(5))
            {
                webhooks = MongoDBInteractor.GetWebhooks();
                lastUpdatedWebhooks = DateTime.UtcNow;
            }

            return webhooks;
        }
        
        public static void SendActivity(DBDifference diff)
        {
            List<ActivityWebhook> activityWebhooks = GetWebhooks();
            if (activityWebhooks.Count <= 0) return;
            foreach (ActivityWebhook activityWebhook in activityWebhooks)
            {
                try
                {
                    switch (activityWebhook.type)
                    {
                        case ActivityWebhookType.Discord:
                            activityWebhook.SendDiscordWebhook(diff);
                            break;
                        case ActivityWebhookType.OculusDB:
                            activityWebhook.SendOculusDBWebhook(diff);
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Log("Couldn't send webhook: " + ex.ToString(), LoggingType.Error);
                    break;
                }
            }
        }
    }
}
