using ComputerUtils.Discord;
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
using System.Text.RegularExpressions;

namespace OculusDB
{
    public class OculusDBServer
    {
        public HttpServer server = null;
        public static Config config { get { return OculusDBEnvironment.config; } set { OculusDBEnvironment.config = value; } }
        public Dictionary<string, string> replace = new Dictionary<string, string>
        {
            {"{meta}", "<meta name=\"theme-color\" content=\"#63fac3\">\n<meta property=\"og:site_name\" content=\"OculusDB\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" }
        };

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

        public static string FormatException(Exception e)
        {
            return e.ToString().Substring(0, e.ToString().Length > 1900 ? 1900 : e.ToString().Length);
        }

        public static void SendMasterWebhookMessage(string title, string description, int color)
        {
            if (config.masterWebhookUrl == "") return;
            try
            {
                Logger.Log("Sending master webhook");
                DiscordWebhook webhook = new DiscordWebhook(config.masterWebhookUrl);
                webhook.SendEmbed(title, description, "master " + DateTime.UtcNow, "OculusDB", config.publicAddress + "logo", config.publicAddress, config.publicAddress + "logo", config.publicAddress, color);
            }
            catch (Exception ex)
            {
                Logger.Log("Exception while sending webhook" + ex.ToString(), LoggingType.Warning);
            }
        }

        public void HandleExeption(object sender, UnhandledExceptionEventArgs args)
        {
            Logger.Log("Unhandled exception has been catched: " + args.ExceptionObject.ToString());
            SendMasterWebhookMessage("Critical Unhandled Exception", "ComputerAnalytics managed to crash. Well done Developer: " + FormatException((Exception)args.ExceptionObject), 0xFF0000);
        }

        public void StartServer(HttpServer httpServer)
        {
            server = httpServer;
            Logger.Log("Working directory is " + OculusDBEnvironment.workingDir);
            Logger.Log("data directory is " + OculusDBEnvironment.dataDir);
            Logger.Log("Starting HttpServer");
            AppDomain.CurrentDomain.UnhandledException += HandleExeption;
            server.StartServer(config.port);

            // Comment if not in dev env
            //server.CacheValidityInSeconds = 0;

            OculusInteractor.Init();
            MongoDBInteractor.Initialize();

            /////////////////////////////////////////////
            // DON'T FORGET TO ADD IT BACK EVERY TIME. //
            /////////////////////////////////////////////
            OculusScraper.StartScrapingThread();

            server.AddRoute("GET", "/api/explore", new Func<ServerRequest, bool>(request =>
            {
                try
                {
                    int count = Convert.ToInt32(request.queryString.Get("count") ?? "50");
                    if (count > 1000) count = 1000;
                    if (count < 0)
                    {
                        request.SendString("[]", "application/json");
                        return true;
                    }
                    int skip = Convert.ToInt32(request.queryString.Get("skip") ?? "0");
                    if (skip < 0) skip = 0;
                    string sorting = (request.queryString.Get("sorting") ?? "name").ToLower();
                    List<BsonDocument> results = new List<BsonDocument>();
                    switch(sorting)
                    {
                        case "reviews":
                            results = MongoDBInteractor.GetBestReviews(skip, count);
                            break;
                        case "name":
                            results = MongoDBInteractor.GetName(skip, count);
                            break;
                        case "publisher":
                            results = MongoDBInteractor.GetPub(skip, count);
                            break;
                        case "releasetime":
                            results = MongoDBInteractor.GetRelease(skip, count);
                            break;
                    }
                    List<dynamic> asObjects = new List<dynamic>();
                    foreach (BsonDocument res in results)
                    {
                        asObjects.Add(ObjectConverter.ConvertToDBType(res));
                    }
                    request.SendString(JsonSerializer.Serialize(asObjects), "application/json");
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString(), LoggingType.Warning);
                    request.SendString("count and skip must be numerical values", "text/plain", 400);
                }
                return true;
            }));
            server.AddRoute("POST", "/api/oculusproxy", new Func<ServerRequest, bool>(request =>
            {
                WebClient webClient = new WebClient();
                webClient.Headers.Add("origin", "https://oculus.com");
                try
                {
                    string res = webClient.UploadString(GraphQLClient.oculusUri + "?" + request.bodyString, "POST", "");
                    request.SendString(res, "application/json", 200, true, new Dictionary<string, string>
                    {
                        {
                            "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                        }
                    });
                } catch(Exception e)
                {
                    string res = webClient.UploadString(GraphQLClient.oculusUri, request.bodyString);
                    request.SendString("{}", "application/json", 500, true, new Dictionary<string, string>
                    {
                        {
                            "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                        }
                    });
                }
                
                return true;
            }));
            server.AddRoute("GET", "/applicationspecific/", new Func<ServerRequest, bool>(request =>
            {
                if(!(new Regex(@"^[0-9]+$").IsMatch(request.pathDiff)))
                {
                    request.SendString("Only application ids are allowed", "text/plain", 400);
                    return true;
                }
                string file = "frontend" + Path.DirectorySeparatorChar + "applicationspecific" + Path.DirectorySeparatorChar + request.pathDiff + ".html";
                if (File.Exists(file))
                {
                    request.SendFile(file);
                    return true;
                }
                request.SendString("No special utilities available", "text/plain", 404);
                return true;
            }), true);
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
                m.test = Updater.GetBaseDir();
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
                // Restarts the scraping thread if it's not running. Putting it here as that's a method often being called while being invoked via the main thread
                OculusScraper.CheckRunning();
                return true;
            }), true);
            server.AddRoute("GET", "/api/search/", new Func<ServerRequest, bool>(request =>
            {
                List<Headset> headsets = new List<Headset>();
                foreach(string h in (request.queryString.Get("headsets") ?? "MONTEREY,RIFT,PACIFIC,GEARVR").Split(','))
                {
                    Headset conv = HeadsetTools.GetHeadsetFromCodeName(h);
                    if(conv != Headset.INVALID) headsets.Add(conv);
                }
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
            server.AddRoute("GET", "/api/pricehistory/", new Func<ServerRequest, bool>(request =>
            {
                List<dynamic> changes = new List<dynamic>();
                foreach (BsonDocument d in MongoDBInteractor.GetPriceChanges(request.pathDiff)) changes.Add(ObjectConverter.ConvertToDBType(d));
                request.SendString(JsonSerializer.Serialize(changes), "application/json");
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
            server.AddRoute("GET", "/api/user", new Func<ServerRequest, bool>(request =>
            {
                try
                {
                    string token = request.queryString.Get("token") ?? "";
                    LoginResponse response = new LoginResponse();
                    if (token != config.masterToken)
                    {
                        response.status = "This user does not exist";
                        request.SendString(JsonSerializer.Serialize(response), "application/json");
                        return true;
                    }
                    response.username = "admin";
                    response.redirect = "/admin";
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
            server.AddRouteFile("/", "frontend" + Path.DirectorySeparatorChar + "home.html", replace, true, true, true);
            server.AddRouteFile("/recentactivity", "frontend" + Path.DirectorySeparatorChar + "recentactivity.html", replace, true, true, true);
            server.AddRouteFile("/server", "frontend" + Path.DirectorySeparatorChar + "server.html", replace, true, true, true);
            server.AddRouteFile("/login", "frontend" + Path.DirectorySeparatorChar + "login.html", replace, true, true, true);
            server.AddRouteFile("/search", "frontend" + Path.DirectorySeparatorChar + "search.html", replace, true, true, true);
            server.AddRouteFile("/logo", "frontend" + Path.DirectorySeparatorChar + "logo.png", true, true, true);
            server.AddRouteFile("/favicon.ico", "frontend" + Path.DirectorySeparatorChar + "favicon.png", true, true, true);
            server.AddRouteFile("/privacy", "frontend" + Path.DirectorySeparatorChar + "privacy.html", replace, true, true, true);
            server.AddRoute("GET", "/admin", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "admin.html"), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRoute("GET", "/console", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "console.html"), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRoute("GET", "/id", new Func<ServerRequest, bool>(request =>
            {
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "id.html").Replace("{0}", request.pathDiff), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRoute("GET", "/activity", new Func<ServerRequest, bool>(request =>
            {
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "activity.html").Replace("{0}", request.pathDiff), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRouteFile("/explore", "frontend" + Path.DirectorySeparatorChar + "explore.html", replace, true, true, true);
            server.AddRouteFile("/script.js", "frontend" + Path.DirectorySeparatorChar + "script.js", replace, true, true, true);
            server.AddRouteFile("/style.css", "frontend" + Path.DirectorySeparatorChar + "style.css", replace, true, true, true);
        }
    }
}