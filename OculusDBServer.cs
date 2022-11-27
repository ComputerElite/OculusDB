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
using ComputerUtils.ConsoleUi;
using ComputerUtils.FileManaging;
using SizeConverter = ComputerUtils.VarUtils.SizeConverter;
using System.Drawing.Imaging;
using OculusDB.Analytics;
using SixLabors.ImageSharp;

namespace OculusDB
{
    public class OculusDBServer
    {
        public HttpServer server = null;
        public static Config config { get { return OculusDBEnvironment.config; } set { OculusDBEnvironment.config = value; } }
        public static bool isBlocked = false;
        // Set to false if not in dev mode
        public static bool debugging = false;
        public Dictionary<string, string> replace = new Dictionary<string, string>
        {
            {"{meta}", "<meta charset=\"UTF-8\">\n<meta name=\"theme-color\" content=\"#63fac3\">\n<meta name=\"site_name\" content=\"OculusDB\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><script async src=\"https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-2771261574362850\" crossorigin=\"anonymous\"></script>" },
            {"{oculusloginlink}", "https://oculus.com/experiences/quest" },
            {"{BSLGDC}", "https://discord.gg/MrwMx5e" },
            {"{OculusDBDC}", "https://discord.gg/zwRfHQN2UY" }
        };
        public static string apiError = "An internal server error occurred. If possible report the issue on the <a href=\"https://discord.gg/zwRfHQN2UY\">OculusDB Discord server</a>. We are sorry for the inconvenience.";

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

        public bool DoesUserHaveAccess(ServerRequest request)
        {
            if (!isBlocked || DateTime.UtcNow >= new DateTime(2022, 7, 7, 15, 0, 0, DateTimeKind.Utc)) return true;
            Cookie code = request.cookies["access"];
            if (code == null || code.Value != config.accesscode)
            {
                Logger.Log("blocked");
                request.Redirect("/blocked");
                return false;
            }
            Logger.Log("not blocked");
            return true;
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
        public static void SendMasterWebhookMessage(string message, string title, string description, int color)
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
			server.StartServer(config.port);
			server.logRequests = false;
            //server.maxRamUsage = 200 * 1024 * 1024; // 200 MB
            Logger.Log("Working directory is " + OculusDBEnvironment.workingDir);
            Logger.Log("data directory is " + OculusDBEnvironment.dataDir);
            Logger.Log("Starting HttpServer");
            FileManager.CreateDirectoryIfNotExisting(OculusDBEnvironment.dataDir + "images");
			Thread t = new Thread(() =>
            {
                Logger.Log("Converting old images", LoggingType.Important);
                foreach (string loc in Directory.EnumerateFiles(OculusDBEnvironment.dataDir + "images"))
                {
                    if (loc.EndsWith(".webp")) continue;
                    try
                    {
                        Logger.Log("Converting " + loc, LoggingType.Important);
                        using (var img = Image.Load(loc))
                        {
                            img.Save(OculusDBEnvironment.dataDir + "images" + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(loc) + ".webp");
                        }
                        File.Delete(loc);
                    } catch
                    {
                        
                    }
                }
            });
            t.Start();
			return;
			AppDomain.CurrentDomain.UnhandledException += HandleExeption;
            FileManager.CreateDirectoryIfNotExisting(OculusDBEnvironment.dataDir + "images");

            OculusInteractor.Init();
            //MongoDBInteractor.Initialize();

            /////////////////////////////////////////////
            // DON'T FORGET TO ADD IT BACK EVERY TIME. //
            /////////////////////////////////////////////
            //OculusScraper.StartScrapingThread();

            //DiscordWebhookSender.SendActivity(DateTime.Now - new TimeSpan(7, 0, 0));

            if(debugging)
            {
                server.DefaultCacheValidityInSeconds = 0;
                server.AddRoute("GET", "/debug/startscrapingthread", new Func<ServerRequest, bool>(request =>
                {
                    OculusScraper.StartGeneralPurposeScrapingThread(false);
                    return true;
                }));
                server.AddRoute("GET", "/debug/addpriority/", new Func<ServerRequest, bool>(request =>
                {
                    OculusScraper.AddApp(request.pathDiff, Headset.HOLLYWOOD, true);
                    return true;
                }), true);
                server.AddRoute("GET", "/debug/addnormal/", new Func<ServerRequest, bool>(request =>
                {
                    OculusScraper.AddApp(request.pathDiff, Headset.HOLLYWOOD, false);
                    return true;
                }), true);
            }

            ////////////////// LOGIN AND ADMIN STUFF
            server.AddRoute("POST", "/api/v1/login", new Func<ServerRequest, bool>(request =>
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
            server.AddRoute("GET", "/api/coremodsproxy", new Func<ServerRequest, bool>(request =>
            {
                WebClient webClient = new WebClient();
                try
                {
                    string res = webClient.DownloadString("https://git.bmbf.dev/unicorns/resources/-/raw/master/com.beatgames.beatsaber/core-mods.json");
                    if (res.Length <= 2) throw new Exception("lol fuck you idiot");
                    File.WriteAllText(OculusDBEnvironment.dataDir + "coremods.json", res);
                    request.SendString(res, "application/json", 200, true, new Dictionary<string, string>
                    {
                        {
                            "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                        }
                    });
                }
                catch (Exception e)
                {
                    if(File.Exists(OculusDBEnvironment.dataDir + "coremods.json"))
                    {
                        request.SendString(File.ReadAllText(OculusDBEnvironment.dataDir + "coremods.json"), "application/json", 200, true, new Dictionary<string, string>
                        {
                            {
                                "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                            }
                        });
                    } else
                    {
                        request.SendString("{}", "application/json", 500, true, new Dictionary<string, string>
                        {
                            {
                                "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                            }
                        });
                    }
                }

                return true;
            }), false, true, true, true, 300); // 5 mins
            server.AddRoute("POST", "/api/v1/checkaccess", new Func<ServerRequest, bool>(request =>
            {
                request.SendString((config.accesscode == request.bodyString).ToString().ToLower());
                return true;
            }));
            server.AddRoute("GET", "/api/v1/user", new Func<ServerRequest, bool>(request =>
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
            server.AddRoute("GET", "/admin", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "admin.html"), "text/html", 200, replace);
                return true;
            }), true, true, true);
            server.AddRouteFile("/login", "frontend" + Path.DirectorySeparatorChar + "login.html", replace, true, true, true, 0, true);
            server.AddRouteFile("/style.css", "frontend" + Path.DirectorySeparatorChar + "style.css", replace, true, true, true, 0, true);
            server.AddRoute("POST", "/api/updateserver/", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                Update u = new Update();
                u.changelog = request.queryString.Get("changelog");
                config.updates.Insert(0, u);
                config.Save();
                request.SendString("Starting update");
                Updater.StartUpdateNetApp(request.bodyBytes, Path.GetFileName(Assembly.GetExecutingAssembly().Location), OculusDBEnvironment.workingDir);
                return true;
            }));
            server.AddRoute("POST", "/api/restartserver/", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString("Restarting");
                Updater.Restart(Path.GetFileName(Assembly.GetExecutingAssembly().Location), OculusDBEnvironment.workingDir);
                return true;
            }));
            server.AddRoute("GET", "/api/servermetrics/", new Func<ServerRequest, bool>(request =>
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
            server.AddRoute("POST", "/api/v1/reportdownload", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                request.SendString(JsonSerializer.Serialize(AnalyticManager.ProcessAnalyticsRequest(request)));
                return true;
            }));
            server.AddRoute("GET", "/api/v1/applicationanalytics/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                DateTime after = DateTime.Parse(request.queryString.Get("after") ?? DateTime.MinValue.ToString());
                int count = Convert.ToInt32(request.queryString.Get("count") ?? "50");
                if (count > 1000) count = 1000;
                if (count <= 0)
                {
                    request.SendString("[]", "application/json");
                    return true;
                }
                int skip = Convert.ToInt32(request.queryString.Get("skip") ?? "0");
                if (skip < 0) skip = 0;
                if (request.pathDiff == "") request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetApplicationAnalytics(after, skip, count)));
                else request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetAllAnalyticsForApplication(request.pathDiff, after)));
                return true;
            }), true, true, true, true, 900); // 15 mins
            server.AddRouteRedirect("GET", "/api/explore/", "/api/v1/explore/");
            server.AddRoute("GET", "/api/v1/explore/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                string sorting = "name";
                int count = 50;
                int skip = 0;
                try
                {
                    count = Convert.ToInt32(request.queryString.Get("count") ?? "50");
                    if (count > 1000) count = 1000;
                    if (count <= 0)
                    {
                        request.SendString("[]", "application/json");
                        return true;
                    }
                    skip = Convert.ToInt32(request.queryString.Get("skip") ?? "0");
                    if (skip < 0) skip = 0;
                    sorting = (request.queryString.Get("sorting") ?? "name").ToLower();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString(), LoggingType.Warning);
                    request.SendString("count and skip must be numerical values", "text/plain", 400);
                }
                try
                {
                    List<BsonDocument> results = new List<BsonDocument>();
                    switch (sorting)
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
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }));
            server.AddRoute("GET", "/applicationspecific/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!(new Regex(@"^[0-9]+$").IsMatch(request.pathDiff)))
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
            }), true, true, true, true);
            
            server.AddRoute("GET", "/api/v1/allapps", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                List<DBApplication> apps = new List<DBApplication>();
                foreach(BsonDocument d in MongoDBInteractor.GetAllApplications())
                {
                    apps.Add(ObjectConverter.ConvertToDBType(d));
                }
                request.SendString(JsonSerializer.Serialize(apps), "application/json");
                return true;
            }), false, true, true, true);
			
			server.AddRoute("GET", "/api/v1/reportmissing/", new Func<ServerRequest, bool>(request =>
			{
				if (!DoesUserHaveAccess(request)) return true;
                string appId = request.pathDiff.Split('?')[0];
                if (appId.EndsWith("/")) appId = appId.TrimEnd('/');
                if(appId.LastIndexOf("/") >= 0) appId = appId.Substring(appId.LastIndexOf("/") + 1);
				Logger.Log(appId);

				Headset h = HeadsetTools.GetHeadsetFromOculusLink(request.pathDiff, Headset.HOLLYWOOD);
				OculusScraper.AddApp(appId, h);
                Data<Application> app = GraphQLClient.GetAppDetail(appId, h);

                if(app.data.node == null)
                {
                    request.SendString("This app does not exist", "text/plain", 400);
                    return true;
                }

				request.SendString("The app has been queued to get added. Allow us up to 5 hours to add the app. Thanks for your collaboration");
				return true;
			}), true, true, true, true);
			
			server.AddRoute("GET", "/api/v1/packagename/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    List<BsonDocument> d = MongoDBInteractor.GetApplicationByPackageName(request.pathDiff);
                    if (d.Count <= 0)
                    {
                        request.SendString("{}", "application/json", 404);
                        return true;
                    }
                    request.SendString(JsonSerializer.Serialize(ObjectConverter.ConvertToDBType(d.First())), "application/json");
                } catch(Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), true, true, true, true);
            server.AddRouteRedirect("GET", "/api/id/", "/api/v1/id/", true);
            server.AddRoute("GET", "/api/v1/id/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    List<BsonDocument> d = MongoDBInteractor.GetByID(request.pathDiff);
                    if (d.Count <= 0)
                    {
                        request.SendString("{}", "application/json", 404);
                        OculusScraper.AddApp(request.pathDiff, Headset.HOLLYWOOD);
                        OculusScraper.AddApp(request.pathDiff, Headset.RIFT);
                        OculusScraper.AddApp(request.pathDiff, Headset.GEARVR);
                        OculusScraper.AddApp(request.pathDiff, Headset.PACIFIC);
                        return true;
                    }
                    request.SendString(JsonSerializer.Serialize(ObjectConverter.ConvertToDBType(d.First())), "application/json");
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                
                return true;
            }), true, true, true, true, 120); // 2 mins
            server.AddRouteRedirect("GET", "/api/connected/", "/api/v1/connected/", true);
            server.AddRoute("GET", "/api/v1/connected/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    ConnectedList connected = MongoDBInteractor.GetConnected(request.pathDiff);
                    request.SendString(JsonSerializer.Serialize(connected), "application/json");
                    // Restarts the scraping thread if it's not running. Putting it here as that's a method often being called while being invoked via the main thread
                    OculusScraper.CheckRunning();

                    // Requests a priority scrape for every app
                    foreach (DBApplication a in connected.applications)
                    {
                        OculusScraper.AddApp(a.id, a.hmd);
                    }
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), true, true, true, true, 360); // 6 mins
            server.AddRouteRedirect("GET", "/api/search/", "/api/v1/search/", true);
            server.AddRoute("GET", "/api/v1/search/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    List<Headset> headsets = new List<Headset>();
                    foreach (string h in (request.queryString.Get("headsets") ?? "MONTEREY,RIFT,PACIFIC,GEARVR").Split(','))
                    {
                        Headset conv = HeadsetTools.GetHeadsetFromCodeName(h);
                        if (conv != Headset.INVALID) headsets.Add(conv);
                    }
                    List<BsonDocument> d = MongoDBInteractor.SearchApplication(request.pathDiff, headsets, request.queryString.Get("quick") == null ? false : true);
                    if (d.Count <= 0)
                    {
                        request.SendString("[]", "application/json", 200);
                        return true;
                    }
                    List<DBApplication> apps = new List<DBApplication>();
                    foreach (BsonDocument doc in d)
                    {
                        apps.Add(ObjectConverter.ConvertToDBType(doc));
                    }
                    request.SendString(JsonSerializer.Serialize(apps), "application/json");
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), true, true, true, true, 360); // 6 mins
            server.AddRouteRedirect("GET", "/api/dlcs/", "/api/v1/dlcs/", true);
            server.AddRoute("GET", "/api/v1/dlcs/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetDLCs(request.pathDiff)), "application/json");
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), true, true, true, true, 360); // 6 mins
            server.AddRouteRedirect("GET", "/api/pricehistory/", "/api/v1/pricehistory/", true);
            server.AddRoute("GET", "/api/v1/pricehistory/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    List<dynamic> changes = new List<dynamic>();
                    foreach (BsonDocument d in MongoDBInteractor.GetPriceChanges(request.pathDiff)) changes.Add(ObjectConverter.ConvertToDBType(d));
                    request.SendString(JsonSerializer.Serialize(changes), "application/json");
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), true, true, true, true, 360); // 6 mins
            server.AddRouteRedirect("GET", "/api/activity/", "/api/v1/activity/");
            server.AddRoute("GET", "/api/v1/activity/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                int count = 50;
                int skip = 0;
                string typeConstraint = "";
                try
                {
                    count = Convert.ToInt32(request.queryString.Get("count") ?? "50");
                    if(count > 1000) count = 1000;
                    if(count < 0)
                    {
                        request.SendString("[]", "application/json");
                        return true;
                    }
                    skip = Convert.ToInt32(request.queryString.Get("skip") ?? "0");
                    if (skip < 0) skip = 0;
                    typeConstraint = request.queryString.Get("typeconstraint") ?? "";
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString(), LoggingType.Warning);
                    request.SendString("count and skip must be numerical values", "text/plain", 400);
                }
                try
                {
                    List<BsonDocument> activities = MongoDBInteractor.GetLatestActivities(count, skip, typeConstraint);
                    List<dynamic> asObjects = new List<dynamic>();
                    foreach (BsonDocument activity in activities)
                    {
                        asObjects.Add(ObjectConverter.ConvertToDBType(activity));
                    }
                    request.SendString(JsonSerializer.Serialize(asObjects), "application/json");
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), false, true, true, true, 30); // 0.5 mins
            server.AddRouteRedirect("GET", "/api/activityid/", "/api/v1/activityid/", true);
            server.AddRoute("GET", "/api/v1/activityid/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    List<BsonDocument> d = MongoDBInteractor.GetActivityById(request.pathDiff);
                    if (d.Count <= 0)
                    {
                        request.SendString("{}", "application/json", 404);
                        return true;
                    }
                    request.SendString(JsonSerializer.Serialize(ObjectConverter.ConvertToDBType(d.First())), "application/json");
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true; 
            }), true, true, true, true);
            server.AddRoute("GET", "/api/serverconsole", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!IsUserAdmin(request)) return true;
                request.SendString(Logger.log);
                return true;
            }));
            server.AddRoute("GET", "/api/config", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!IsUserAdmin(request)) return true;
                request.SendString(JsonSerializer.Serialize(config));
                return true;
            }));
            server.AddRoute("GET", "/api/v1/updates", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                WebClient c = new WebClient();
                c.Headers.Add("User-Agent", "OculusDB/1.0");
                List<GithubCommit> commits = JsonSerializer.Deserialize<List<GithubCommit>>(c.DownloadString("https://api.github.com/repos/ComputerElite/OculusDB/commits?per_page=100"));
                List<Update> updates = new List<Update>();
                foreach(GithubCommit co in commits)
                {
                    updates.Add(new Update { changelog = co.commit.message + "\\n\\nFull changes: " + co.html_url, time = co.commit.committer.date });
                }
                request.SendString(JsonSerializer.Serialize(updates));
                return true;
            }), false, true, true, true, 3600); // 1 hour
            server.AddRoute("GET", "/api/v1/database", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    DBInfo info = new DBInfo();
                    info.currentUpdateStart = config.ScrapingResumeData.currentScrapeStart;
                    info.lastUpdated = config.lastDBUpdate;
                    info.appsToScrape = MongoDBInteractor.GetAppsToScrapeCount(false);
                    info.scrapedApps = MongoDBInteractor.GetScrapedAppsCount(false);
                    info.dataDocuments = MongoDBInteractor.CountDataDocuments();
                    info.activityDocuments = MongoDBInteractor.CountActivityDocuments();
                    request.SendString(JsonSerializer.Serialize(info));
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }));
            server.AddRoute("POST", "/api/config", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!IsUserAdmin(request)) return true;
                config = JsonSerializer.Deserialize<Config>(request.bodyString);
                config.Save();
                request.SendString("Updated config");
                return true;
            }));
            server.AddRoute("GET", "/cdn/images/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!(new Regex(@"^[0-9]+$").IsMatch(request.pathDiff)))
                {
                    request.SendString("Only application ids are allowed", "text/plain", 400);
                    return true;
                }
                request.SendFile(OculusDBEnvironment.dataDir + "images" + Path.DirectorySeparatorChar + request.pathDiff + ".webp");
                return true;
            }), true, true, true, true, 1800, true); // 30 mins
            ////////////// ACCESS CHECK IF OCULUSDB IS BLOCKED
            Func<ServerRequest, bool> accessCheck = null;
            /*
            new Func<ServerRequest, bool>(request =>
            {
                return DoesUserHaveAccess(request);
            });
            */

            server.AddRouteFile("/", "frontend" + Path.DirectorySeparatorChar + "home.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/recentactivity", "frontend" + Path.DirectorySeparatorChar + "recentactivity.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/server", "frontend" + Path.DirectorySeparatorChar + "server.html", replace, true, true, true, accessCheck);
            
            server.AddRouteFile("/downloadstats", "frontend" + Path.DirectorySeparatorChar + "downloadstats.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/search", "frontend" + Path.DirectorySeparatorChar + "search.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/logo", "frontend" + Path.DirectorySeparatorChar + "logo.png", true, true, true, accessCheck);
            server.AddRouteFile("/notfound.jpg", "frontend" + Path.DirectorySeparatorChar + "notfound.jpg", true, true, true, accessCheck);
            server.AddRouteFile("/favicon.ico", "frontend" + Path.DirectorySeparatorChar + "favicon.png", true, true, true, accessCheck);
            server.AddRouteFile("/privacy", "frontend" + Path.DirectorySeparatorChar + "privacy.html", replace, true, true, true, accessCheck);
            
            server.AddRoute("GET", "/console", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!IsUserAdmin(request)) return true;
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "console.html"), "text/html", 200, replace);
                return true;
            }), true, true, true);
            server.AddRoute("GET", "/id/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "id.html").Replace("{0}", request.pathDiff), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRoute("GET", "/activity", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                request.SendStringReplace(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "activity.html").Replace("{0}", request.pathDiff), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRouteFile("/explore", "frontend" + Path.DirectorySeparatorChar + "explore.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/script.js", "frontend" + Path.DirectorySeparatorChar + "script.js", replace, true, true, true, accessCheck);
            server.AddRouteFile("/api/docs", "frontend" + Path.DirectorySeparatorChar + "api.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/jsonview.js", "frontend" + Path.DirectorySeparatorChar + "jsonview.js", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide", "frontend" + Path.DirectorySeparatorChar + "guide.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/supportus", "frontend" + Path.DirectorySeparatorChar + "supportus.html", replace, true, true, true, accessCheck);

            // for all the annoying people out there4
            server.AddRouteRedirect("GET", "/idiot", "/guide/quest");

            server.AddRouteFile("/guide/quest", "frontend" + Path.DirectorySeparatorChar + "guidequest.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide/quest/pc", "frontend" + Path.DirectorySeparatorChar + "guidequest_PC.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide/quest/qavs", "frontend" + Path.DirectorySeparatorChar + "guidequest_QAVS.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide/quest/sqq", "frontend" + Path.DirectorySeparatorChar + "guidequest_SQQ.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/assets/sq.png", "frontend" + Path.DirectorySeparatorChar + "sq.png", true, true, true, accessCheck);
            server.AddRouteFile("/assets/discord.svg", "frontend" + Path.DirectorySeparatorChar + "discord.svg", true, true, true, accessCheck);


            server.AddRouteFile("/guide/rift", "frontend" + Path.DirectorySeparatorChar + "guiderift.html", replace, true, true, true, accessCheck);
            server.AddRoute("GET", "/api/api.json", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                request.SendString(File.ReadAllText("frontend" + Path.DirectorySeparatorChar + "api.json").Replace("\n", ""), "application/json", 200);
                return true;
            }), true, true, true, true);

            ///////////////////// BLOCK OCULUSDB HERE
            if (isBlocked)
            {
                // Block entire OculusDB
                server.AddRoute("GET", "/blocked", new Func<ServerRequest, bool>(request =>
                {
                    request.SendFile("frontend" + Path.DirectorySeparatorChar + "blocked.html", replace);
                    return true;
                }), true, true, true, true);
                //return;
            }


            //// jokes are fun
            server.AddRouteFile("/cdn/boom.ogg", "frontend" + Path.DirectorySeparatorChar + "assets" + Path.DirectorySeparatorChar + "boom.ogg", true, true, true, accessCheck);
            server.AddRouteFile("/cdn/modem.ogg", "frontend" + Path.DirectorySeparatorChar + "assets" + Path.DirectorySeparatorChar + "modem.ogg", true, true, true, accessCheck);

            server.AddRouteFile("/cdn/BS2.jpg", "frontend" + Path.DirectorySeparatorChar + "assets" + Path.DirectorySeparatorChar + "BS2.jpg", true, true, true, accessCheck);

        }
    }
}
