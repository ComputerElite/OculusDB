using System.Text.Json;
using ComputerUtils.Logging;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB;

public class OculusDBTest
{
    public static void Test()
    {
        GraphQLClient.log = false;
        Application app = GraphQLClient.AppDetailsDeveloperAll("2448060205267927").data.node;
        Logger.Log(JsonSerializer.Serialize(OculusConverter.Application(app)));
    }
}