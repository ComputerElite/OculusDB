using ComputerUtils.Logging;
using ComputerUtils.Updating;
using ComputerUtils.VarUtils;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using OculusDB.Database;
using OculusDB.Users;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace OculusDB
{
    public class OculusDBServer
    {
        public HttpServer server = null;
        public Config config { get { return OculusDBEnvironment.config; } set { OculusDBEnvironment.config = value; } }

        public string GetToken(ServerRequest request, bool send403 = true)
        {
            Cookie token = request.cookies["token"];
            if (token == null)
            {
                if(send403) request.Send403();
                return "";
            }
            return token.Value;
        }

        public bool IsUserAdmin(ServerRequest request, bool send403 = true)
        {
            return GetToken(request, send403) == config.masterToken;
        }

        public void StartServer(HttpServer httpServer)
        {
            server = httpServer;
            Logger.Log("Working directory is " + OculusDBEnvironment.workingDir);
            Logger.Log("data directory is " + OculusDBEnvironment.dataDir);
            Logger.Log("Starting HttpServer");
            server.StartServer(config.port);

            OculusInteractor.Init();
            MongoDBInteractor.Initialize();
            OculusScraper.StartScrapingThread();

            server.AddRoute("POST", "/api/updateserver", new Func<ServerRequest, bool>(request =>
            {
                if(!IsUserAdmin(request)) return true;
                Update u = new Update();
                u.changelog = request.queryString.Get("changelog");
                config.updates.Insert(0, u);
                config.Save();
                request.SendString("Starting update");
                Updater.StartUpdateNetApp(request.bodyBytes, Path.GetFileName(Assembly.GetExecutingAssembly().Location), OculusDBEnvironment.workingDir);
                return true;
            }));
            server.AddRoute("POST", "/api/restartserver", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString("Restarting");
                Updater.Restart(Path.GetFileName(Assembly.GetExecutingAssembly().Location), OculusDBEnvironment.workingDir);
                return true;
            }));
            server.AddRoute("GET", "/api/servermetrics", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                ServerMetrics m = new ServerMetrics();
                Process currentProcess = Process.GetCurrentProcess();
                m.ramUsage = currentProcess.WorkingSet64;
                m.ramUsageString = SizeConverter.ByteSizeToString(m.ramUsage);
                m.workingDirectory = OculusDBEnvironment.workingDir;
                request.SendString(JsonSerializer.Serialize(m), "application/json");
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
                List<Headset> headsets = new List<Headset> { Headset.GEARVR, Headset.MONTEREY, Headset.PACIFIC, Headset.RIFT };
                List<BsonDocument> d = MongoDBInteractor.SearchApplication(request.pathDiff, headsets);
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
            server.AddRoute("GET", "/api/activity", new Func<ServerRequest, bool>(request =>
            {
                try
                {
                    int count = Convert.ToInt32(request.queryString.Get("count") ?? "50");
                    if(count > 1000) count = 1000;
                    if(count < 0)
                    {
                        request.SendString("[]", "application/json");
                        return true;
                    }
                    int skip = Convert.ToInt32(request.queryString.Get("skip") ?? "0");
                    if (skip < 0) skip = 0;
                    string typeConstraint = request.queryString.Get("typeconstraint") ?? "";
                    List<BsonDocument> activities = MongoDBInteractor.GetLatestActivities(count, skip, typeConstraint);
                    List<dynamic> asObjects = new List<dynamic>(); 
                    foreach(BsonDocument activity in activities)
                    {
                        asObjects.Add(ObjectConverter.ConvertToDBType(activity));
                    }
                    request.SendString(JsonSerializer.Serialize(asObjects), "application/json");
                } catch (Exception ex)
                {
                    Logger.Log(ex.ToString(), LoggingType.Warning);
                    request.SendString("count and skip must be numerical values", "text/plain", 400);
                }
               
                return true;
            }));
            server.AddRoute("GET", "/api/activityid", new Func<ServerRequest, bool>(request =>
            {
                List<BsonDocument> d = MongoDBInteractor.GetActivityById(request.pathDiff);
                if (d.Count <= 0)
                {
                    request.SendString("{}", "application/json", 404);
                    return true;
                }
                request.SendString(JsonSerializer.Serialize(ObjectConverter.ConvertToDBType(d.First())), "application/json");
                return true; 
            }), true);
            server.AddRoute("POST", "/api/login", new Func<ServerRequest, bool>(request =>
            {
                try
                {
                    LoginRequest loginRequest = JsonSerializer.Deserialize<LoginRequest>(request.bodyString);
                    LoginResponse response = new LoginResponse();
                    if (loginRequest.password != config.masterToken)
                    {
                        response.status = "This user does not exist";
                        request.SendString(JsonSerializer.Serialize(response), "application/json");
                        return true;
                    }
                    response.username = "admin";
                    response.redirect = "/admin";
                    response.token = config.masterToken;
                    response.authorized = true;
                    request.SendString(JsonSerializer.Serialize(response), "application/json");
                }
                catch
                {
                    request.SendString("{}", "application/json");
                }
                return true;
            }));
            server.AddRoute("GET", "/api/serverconsole", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString(Logger.log);
                return true;
            }));
            server.AddRoute("GET", "/api/config", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString(JsonSerializer.Serialize(config));
                return true;
            }));
            server.AddRoute("GET", "/api/updates", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(config.updates.Take(50)));
                return true;
            }));
            server.AddRoute("GET", "/api/database", new Func<ServerRequest, bool>(request =>
            {
                DBInfo info = new DBInfo();
                info.currentUpdateStart = config.ScrapingResumeData.currentScrapeStart;
                info.lastUpdated = config.lastDBUpdate;
                info.dataDocuments = MongoDBInteractor.CountDataDocuments();
                info.activityDocuments = MongoDBInteractor.CountActivityDocuments();
                request.SendString(JsonSerializer.Serialize(info));
                return true;
            }));
            server.AddRoute("POST", "/api/config", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                config = JsonSerializer.Deserialize<Config>(request.bodyString);
                request.SendString("Updated config");
                return true;
            }));
            server.AddRouteFile("/", "frontend\\home.html");
            server.AddRouteFile("/recentactivity", "frontend\\recentactivity.html");
            server.AddRouteFile("/server", "frontend\\server.html");
            server.AddRouteFile("/login", "frontend\\login.html");
            server.AddRouteFile("/search", "frontend\\search.html");
            server.AddRouteFile("/logo", "frontend\\logo.png");
            server.AddRouteFile("/favicon.ico", "frontend\\favicon.png");
            server.AddRoute("GET", "/admin", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString(File.ReadAllText("frontend\\admin.html"), "text/html");
                return true;
            }), true);
            server.AddRoute("GET", "/console", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString(File.ReadAllText("frontend\\console.html"), "text/html");
                return true;
            }), true);
            server.AddRoute("GET", "/id", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(File.ReadAllText("frontend\\id.html").Replace("{0}", request.pathDiff), "text/html");
                return true;
            }), true);
            server.AddRoute("GET", "/activity", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(File.ReadAllText("frontend\\activity.html").Replace("{0}", request.pathDiff), "text/html");
                return true;
            }), true);
            server.AddRouteFile("/script.js", "frontend\\script.js");
            server.AddRouteFile("/style.css", "frontend\\style.css");
        }
    }
}