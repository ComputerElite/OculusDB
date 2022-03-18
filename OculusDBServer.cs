using ComputerUtils.Logging;
using ComputerUtils.Updating;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using OculusDB.Database;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System.Reflection;
using System.Text.Json;

namespace OculusDB
{
    public class OculusDBServer
    {
        public HttpServer server = null;
        public Config config { get { return OculusDBEnvironment.config; } }

        public bool IsUserAdmin()
        {
            return true;
        }

        public void StartServer(HttpServer httpServer)
        {
            server = httpServer;
            Logger.Log("Working directory is " + OculusDBEnvironment.workingDir);
            Logger.Log("data directory is " + OculusDBEnvironment.dataDir);
            Logger.Log("Starting HttpServer");
            server.StartServer(config.port);
            Logger.Log("Public address: " + config.publicAddress);

            OculusInteractor.Init();
            MongoDBInteractor.Initialize();

            server.AddRoute("POST", "/updateserver", new Func<ServerRequest, bool>(request =>
            {
                if(IsUserAdmin())
                {
                    request.Send403();
                    return true;
                }
                Updater.StartUpdateNetApp(request.bodyBytes, Assembly.GetExecutingAssembly().GetName().FullName, OculusDBEnvironment.workingDir);
                return true;
            }));
            server.AddRoute("GET", "/api/id/", new Func<ServerRequest, bool>(request =>
            {
                List<BsonDocument> d = MongoDBInteractor.GetByID(request.pathDiff);
                if(d.Count <= 0)
                {
                    request.SendString("{}", "application/json", 404);
                    return true;
                }
                request.SendString(JsonSerializer.Serialize(ObjectConverter.ConvertToDBType(d.First())), "application/json");
                return true;
            }), true);
            server.AddRoute("GET", "/api/connected/", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetConnected(request.pathDiff)), "application/json");
                return true;
            }), true);
            server.AddRoute("GET", "/api/search/", new Func<ServerRequest, bool>(request =>
            {
                List<BsonDocument> d = MongoDBInteractor.SearchApplication(request.pathDiff);
                if (d.Count <= 0)
                {
                    request.SendString("[]", "application/json", 200);
                    return true;
                }
                List<DBApplication> apps = new List<DBApplication>();
                foreach(BsonDocument doc in d)
                {
                    apps.Add(ObjectConverter.ConvertToDBType(doc));
                }
                request.SendString(JsonSerializer.Serialize(apps), "application/json");
                return true;
            }), true);
            server.AddRoute("GET", "/api/dlcs/", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetDLCs(request.pathDiff)), "application/json");
                return true;
            }), true);
            server.AddRouteFile("/", "..\\..\\..\\home.html");
            server.AddRouteFile("/search", "..\\..\\..\\search.html");
            server.AddRoute("GET", "/id", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(File.ReadAllText("..\\..\\..\\id.html").Replace("{0}", request.pathDiff), "text/html");
                return true;
            }), true);
            server.AddRouteFile("/script.js", "..\\..\\..\\script.js");
            server.AddRouteFile("/style.css", "..\\..\\..\\style.css");
            server.AddRoute("GET", "/addallappsquest", new Func<ServerRequest, bool>(request =>
            {
                request.SendString("Adding apps");
                DateTime lastUpdate = DateTime.Now;
                foreach (Application a in OculusInteractor.EnumerateAllApplicationsDetail(Headset.MONTEREY))
                {
                    //Logger.Log(JsonSerializer.Serialize(a));
                    //Logger.Log(a.latest_supported_binary.firstIapItems.edges.Count.ToString() + " DLCs found");
                    MongoDBInteractor.AddApplication(a);
                    Data<Application> d = GraphQLClient.GetDLCs(a.id);
                    foreach(AndroidBinary b in GraphQLClient.AllVersionsOfApp(a.id).data.node.primary_binaries.nodes)
                    {
                        MongoDBInteractor.AddVersion(b, a);
                    }
                    if (d.data.node.latest_supported_binary.firstIapItems == null) continue;
                    Logger.Log("Adding " + d.data.node.latest_supported_binary.firstIapItems.edges.Count + " of " + d.data.node.latest_supported_binary.firstIapItems.count + " DLCs");
                    foreach (Node<AppItemBundle> dlc in d.data.node.latest_supported_binary.firstIapItems.edges)
                    {
                        Logger.Log("Adding dlc " + dlc.node.id);
                        if (dlc.node.IsIAPItem()) MongoDBInteractor.AddDLC(dlc.node);
                        else MongoDBInteractor.AddDLCPack(dlc.node);
                    }
                }
                config.lastDBUpdate = lastUpdate;
                config.Save();
                return true;
            }));
            server.AddWSRoute("/", new Action<SocketServerRequest>(request =>
            {
                if(request.bodyString == "allversions")
                {
                    foreach(Application a in OculusInteractor.EnumerateAllApplicationsDetail(Headset.MONTEREY))
                    {
                        request.SendString(JsonSerializer.Serialize(a));
                        if (request.handler.closed)
                        {
                            Logger.Log("Stopping requests", LoggingType.Debug);
                            break;
                        }
                    }
                }
            }));
        }
    }
}