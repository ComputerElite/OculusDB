using ComputerUtils.Webserver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusDB.Analytics
{
    public class AnalyticManager
    {
        public static List<string> lastFewAnalyticsOrigins = new List<string>();
        public static AnalyticResponse ProcessAnalyticsRequest(ServerRequest request)
        {
            lastFewAnalyticsOrigins.Add(request.remote);
            if (lastFewAnalyticsOrigins.Count > 100) lastFewAnalyticsOrigins.RemoveAt(0);
            if (lastFewAnalyticsOrigins.Where(x => x == request.remote).Count() > 50) return new AnalyticResponse
            {
                msg = "Too many requests. Try again later"
            };
            AnalyticRequest ar = JsonSerializer.Deserialize<AnalyticRequest>(request.bodyString);
            Analytic a = new Analytic();
            a.itemId = ar.id;
            List<BsonDocument> entry = MongoDBInteractor.GetByID(a.itemId);
            if (entry.Count <= 0) return new AnalyticResponse
            {
                msg = "The item hasn't been found in the database"
            };
            if (entry[0]["parentApplication"] == null) return new AnalyticResponse
            {
                msg = "Item doesn't have parentApplication"
            };
            a.parentId = entry[0]["parentApplication"]["id"].AsString;
            a.applicationName = entry[0]["parentApplication"]["displayName"].AsString;
            MongoDBInteractor.AddAnalytic(a);
            return new AnalyticResponse
            {
                success = true,
                msg = "Added analytic. Only the version/dlc id you clicked as well as the current time has been recorded. Nothing else"
            };
        }
    }
}
