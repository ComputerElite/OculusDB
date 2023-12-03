using ComputerUtils.Logging;
using OculusDB.Database;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusDB
{
    public class OculusInteractor
    {
        public static bool logOculusRequests = false;
        public static void Init()
        {
            GraphQLClient.forcedLocale = "en_US";
            GraphQLClient.throwException = false;
            GraphQLClient.log = logOculusRequests;
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
        
        public static IEnumerable<IAPItem> EnumerateAllDLCs(string groupingId)
        {
            Data<ApplicationGrouping?> s = GraphQLClient.GetDLCsDeveloper(groupingId);
            if(s.data.node == null)
            {
                throw new Exception("Could not get data to enumerate dlcs.");
            }
            while (true)
            {
                foreach (Node<IAPItem> e in s.data.node.add_ons.edges)
                {
                    yield return e.node;
                }

                if (!s.data.node.add_ons.page_info.has_next_page) break;
                s = GraphQLClient.GetDLCsDeveloper(groupingId, s.data.node.add_ons.page_info.end_cursor);
            }
        }
        
        public static IEnumerable<AchievementDefinition> EnumerateAllAchievements(string appId)
        {
            Data<Application?> s = GraphQLClient.GetAchievements(appId);
            Logger.Log(JsonSerializer.Serialize(s));
            if(s.data.node == null)
            {
                throw new Exception("Could not get data to enumerate achievements.");
            }
            foreach (AchievementDefinition e in s.data.node.grouping.achievement_definitions.nodes)
            {
                yield return e;
            }
        }

        public static IEnumerable<OculusBinary> EnumerateAllVersions(string appId)
        {
            Data<EdgesPrimaryBinaryApplication> s = GraphQLClient.AllVersionOfAppCursor(appId);
            if(s.data.node == null)
            {
                throw new Exception("Could not get data to enumerate dlcs.");
            }
            while (true)
            {
                foreach (Node<OculusBinary> e in s.data.node.primary_binaries.edges)
                {
                    yield return e.node;
                }

                if (!s.data.node.primary_binaries.page_info.has_next_page) break;
                s = GraphQLClient.AllVersionOfAppCursor(appId, s.data.node.primary_binaries.page_info.end_cursor);
            }
        }
    }
}
