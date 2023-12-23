using ComputerUtils.Webserver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OculusDB.Database;
using OculusDB.MongoDB;

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
            DBBase? entry = OculusDBDatabase.GetDocument(a.itemId);
            if (entry == null) return new AnalyticResponse
            {
                msg = "The item hasn't been found in the database"
            };
            string parentId = "";
            string parentName = "";
            switch (entry.__OculusDBType)
            {
                case DBDataTypes.Version:
                    parentName = ((DBVersion)entry).parentApplication?.id ?? "";
                    parentId = ((DBVersion)entry).parentApplication?.id ?? "";
                    break;
                default:
                    return new AnalyticResponse
                    {
                        msg = "Only versions are recorded for now. This may change in the future"
                    };
            }

            a.parentId = parentId;
            a.applicationName = parentName;
            MongoDBInteractor.AddAnalytic(a);
            return new AnalyticResponse
            {
                success = true,
                msg = "Added analytic. Only the version id you clicked as well as the current time has been recorded. Nothing else"
            };
        }
    }
}
