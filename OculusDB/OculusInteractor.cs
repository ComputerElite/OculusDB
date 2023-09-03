using ComputerUtils.Logging;
using OculusDB.Database;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusDB
{
    public class OculusInteractor
    {
        public static void Init()
        {
            GraphQLClient.forcedLocale = "en_US";
            GraphQLClient.throwException = false;
            GraphQLClient.log = false;
        }

        public static IEnumerable<Application> EnumerateAllApplications(Headset headset)
        {
            Data<AppStoreAllAppsSection> s = GraphQLClient.AllApps(headset);
            int i = 0;
            if(s.data.node == null)
            {
                throw new Exception("Could not get data to enumerate applications.");
            }
            while (i < s.data.node.all_items.count)
            {
                string cursor = "";
                foreach (Node<Application> e in s.data.node.all_items.edges)
                {
                    cursor = e.cursor;
                    i++;
                    yield return e.node;
                }
                s = GraphQLClient.AllApps(headset, cursor);
            }
        }
    }
}
