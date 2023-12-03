using System.Text.Json;
using ComputerUtils.Logging;
using OculusDB.Database;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB;

public class OculusDBTest
{
    public static void Test()
    {
        GraphQLClient.log = false;
        string scrapingNodeName = "Fuck Oculus node";
        // Add application
        Application app = GraphQLClient.AppDetailsDeveloperAll("2448060205267927").data.node;
        DBApplication dbApp = OculusConverter.AddScrapingNodeName(OculusConverter.Application(app), scrapingNodeName);

        // Add Achievements
        List<DBAchievement> achievements = new List<DBAchievement>();
        foreach (AchievementDefinition achievement in OculusInteractor.EnumerateAllAchievements(dbApp.id))
        {
            achievements.Add(OculusConverter.AddScrapingNodeName(OculusConverter.Achievement(achievement, dbApp), scrapingNodeName));
        }
        Logger.Log(JsonSerializer.Serialize(achievements));

        return;
        // Add DLCs
        List<DBIAPItem> iapItems = new List<DBIAPItem>();
        int i = 0;
        foreach (IAPItem iapItem in OculusInteractor.EnumerateAllDLCs(dbApp.grouping.id))
        {
            Logger.Log(i.ToString());
            i++;
            iapItems.Add(OculusConverter.AddScrapingNodeName(OculusConverter.IAPItem(iapItem, dbApp), scrapingNodeName));
        }
        Logger.Log(JsonSerializer.Serialize(iapItems));
        
        //Logger.Log(JsonSerializer.Serialize(OculusConverter.AddScrapingNodeName(dbApp, "Fuck Oculus")));
        //DBVersion version = OculusConverter.Version(GraphQLClient.GetMoreBinaryDetails("24162327616747873").data.node, app, new List<VersionAlias>());
        //Logger.Log(JsonSerializer.Serialize(version));
    }
}