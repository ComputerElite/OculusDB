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
        Application app = GraphQLClient.AppDetailsDeveloperAll("2448060205267927").data.node;
        DBApplication dbApp = OculusConverter.Application(app);
        //Logger.Log(JsonSerializer.Serialize(dbApp));
        DBVersion version = OculusConverter.Version(GraphQLClient.GetMoreBinaryDetails("24162327616747873").data.node, app, new List<VersionAlias>());
        Logger.Log(JsonSerializer.Serialize(version));
    }
}