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
                List<DiscordActivityWebhook> activityWebhooks = MongoDBInteractor.GetWebhooks();
                if (activityWebhooks.Count <= 0) return;
                List<BsonDocument> activities = MongoDBInteractor.GetLatestActivities(start);
                foreach (DiscordActivityWebhook activityWebhook in activityWebhooks)
                {
                    foreach (BsonDocument activity in activities)
                    {
                        try
                        {
                            activityWebhook.SendWebhook(activity);
                        }
                        catch (Exception ex)
                        {
                            Logger.Log("Couldn't send webhook: " + ex.ToString(), LoggingType.Error);
                        }
                    }
                }
            });
            t.Start();
        }
    }
}
