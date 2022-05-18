using ComputerUtils.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                List<BsonDocument> activities = MongoDBInteractor.GetLatestActivities(start);
                foreach (ActivityWebhook activityWebhook in activityWebhooks)
                {
                    foreach (BsonDocument activity in activities)
                    {
                        try
                        {
                            switch(activityWebhook.type)
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
    }
}
