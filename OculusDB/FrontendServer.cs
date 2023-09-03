using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using ComputerUtils.Discord;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using ComputerUtils.Updating;
using ComputerUtils.VarUtils;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using MongoDB.Driver.Core.WireProtocol.Messages;
using OculusDB.Analytics;
using OculusDB.Database;
using OculusDB.QAVS;
using OculusDB.ScrapingMaster;
using OculusDB.ScrapingNodeCode;
using OculusDB.Users;
using OculusGraphQLApiLib;

namespace OculusDB;

public class FrontendServer
{
    public HttpServer server = null;
        public static Config config { get { return OculusDBEnvironment.config; } set { OculusDBEnvironment.config = value; } }
        public static bool isBlocked = false;
        public static Dictionary<string, string> replace = new Dictionary<string, string>
        {
            {"{meta}", "<meta charset=\"UTF-8\">\n<meta name=\"theme-color\" content=\"#63fac3\">\n<meta name=\"site_name\" content=\"OculusDB\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" },
            {"{oculusloginlink}", "https://developer.oculus.com/manage/" },
            {"{BSLGDC}", "https://discord.gg/MrwMx5e" },
            {"{OculusDBDC}", "https://discord.gg/zwRfHQN2UY" },
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

        public void StartServer(HttpServer httpServer)
        {
            server = httpServer;
			server.StartServer(config.port);
			server.logRequests = true;
            server.maxRamUsage = 700 * 1024 * 1024; // 700 MB
            Logger.Log("Working directory is " + OculusDBEnvironment.workingDir);
            Logger.Log("data directory is " + OculusDBEnvironment.dataDir);
            FileManager.CreateDirectoryIfNotExisting(OculusDBEnvironment.dataDir + "images");

            
            Logger.Log("Initializing MongoDB");
			MongoDBInteractor.Initialize();
            ScrapingNodeMongoDBManager.Init();

            Logger.Log("Setting up routes");
            string frontend = OculusDBEnvironment.debugging ? @"..\..\..\frontend\" : "frontend" + Path.DirectorySeparatorChar;
            
            ////////////////// Admin
            server.AddRoute("GET", "/api/v1/admin/users", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!IsUserAdmin(request)) return true;
                request.SendString(JsonSerializer.Serialize(config.tokens));
                return true;
            }));
            server.AddRoute("POST", "/api/v1/admin/users", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!IsUserAdmin(request)) return true;
                config.tokens = JsonSerializer.Deserialize<List<Token>>(request.bodyString);
                request.SendString("Set users");
                return true;
            }));
            
            server.AddRoute("GET", "/api/v1/blocked/blockedapps", request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetBlockedApps()), "application/json");
                return true;
            });
            server.AddRoute("DELETE", "/api/v1/blocked/unblockapp", request =>
            {
                string id = request.queryString.Get("id");
                if (!DoesTokenHaveAccess(request, Permission.BlockApps))
                {
                    return true;
                }
                MongoDBInteractor.UnblockApp(id);
                request.SendString("unblocked " + id, "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/v1/createscrapingnode", request =>
            {
                string id = request.queryString.Get("id");
                string name = request.queryString.Get("name");
                if (!DoesTokenHaveAccess(request, Permission.CreateScrapingNode))
                {
                    return true;
                }
                request.SendString(ScrapingNodeMongoDBManager.CreateScrapingNode(id, name), "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/v1/blocked/blockapp", request =>
            {
                string id = request.queryString.Get("id");
                if (!DoesTokenHaveAccess(request, Permission.BlockApps))
                {
                    return true;
                }
                MongoDBInteractor.BlockApp(id);
                request.SendString("blocked " + id, "application/json");
                return true;
            });
            server.AddRoute("POST", "/api/v1/admin/scrape/", request =>
            {
                AppToScrape s = JsonSerializer.Deserialize<AppToScrape>(request.bodyString);
                if (!DoesTokenHaveAccess(request, s.priority ? Permission.StartPriorityScrapes : Permission.StartScrapes))
                {
                    return true;
                }
                try
                {
                    // Create scraping node for scraping, start that node and supply one task to it
                    ScrapingNodeMongoDBManager.AddApp(s, AppScrapePriority.High);
                    request.SendString("Added scrape to queue as first to be scraped. No idea if it'll scrape or not but a scraping node will defo try.");
                }
                catch (Exception e)
                {
                    request.SendString(e.ToString(), "text/plain", 500);
                }
                return true;
            });
            server.AddRouteFile("/utils", frontend + "utils.html", replace);

            ////////////////// Aliases
            server.AddRoute("GET", "/api/v1/aliases/applications", new Func<ServerRequest, bool>(request =>
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetApplicationsWithAliases()));
                return true;
            }));
			server.AddRoute("POST", "/api/v1/aliases/", new Func<ServerRequest, bool>(request =>
			{
                if (!IsUserAdmin(request)) return true;
                List<VersionAlias> aliases = JsonSerializer.Deserialize<List<VersionAlias>>(request.bodyString);
                foreach(VersionAlias a in aliases)
                {
                    MongoDBInteractor.AddVersionAlias(a);
                }
                request.SendString("Added aliases");
				return true;
			}));

			////////////////// LOGIN AND ADMIN STUFF
			server.AddRoute("POST", "/api/v1/login", new Func<ServerRequest, bool>(request =>
            {
                try
                {
                    LoginRequest loginRequest = JsonSerializer.Deserialize<LoginRequest>(request.bodyString);
                    LoginResponse response = new LoginResponse();
                    if (loginRequest.password == config.masterToken)
                    {
                        
                        response.isAdmin = true;
                        response.authorized = true;
                        response.username = "admin";
                        response.redirect = "/admin";
                        response.token = config.masterToken;
                        request.SendString(JsonSerializer.Serialize(response), "application/json");
                        return true;
                    }

                    foreach (Token token in config.tokens)
                    {
                        if (token.token == loginRequest.password)
                        {
                            response.isAdmin = false;
                            response.authorized = true;
                            response.username = "Token";
                            response.redirect = "/utils";
                            response.token = token.token;
                            request.SendString(JsonSerializer.Serialize(response), "application/json");
                            return true;
                        }
                    }
                
                    response.status = "This user does not exist";
                    request.SendString(JsonSerializer.Serialize(response), "application/json");
                    return true;
                }
                catch
                {
                    request.SendString("{}", "application/json");
                }
                return true;
            }));
            server.AddRoute("GET", "/api/coremodsdownload/", new Func<ServerRequest, bool>(request =>
            {
	            string v = request.pathDiff.Replace(".qmod", "");
				Dictionary<string, CoreMods> mods = JsonSerializer.Deserialize<Dictionary<string, CoreMods>>(File.ReadAllText(OculusDBEnvironment.dataDir + "coremods.json"));
                if(mods.ContainsKey(v))
                {
                    CoreMods used = mods[v];
                    QMod mod = new QMod();
					mod.name = "Core mods for " + v;
                    mod.id = "OculusDB_CoreMods_" + v;
                    mod.packageVersion = v;
                    mod.description = "Downloads all Core mods for Beat Saber version " + v;
					foreach (CoreMod m in used.mods)
                    {
                        mod.dependencies.Add(new QModDependency { downloadIfMissing = m.downloadLink, id = m.id, version = "^" + m.version });
                    }
                    MemoryStream stream = new MemoryStream();
                    ZipArchive a = new ZipArchive(stream, ZipArchiveMode.Create, true);
                    StreamWriter writer = new StreamWriter(a.CreateEntry("mod.json").Open());
                    writer.Write(JsonSerializer.Serialize(mod));
                    writer.Flush();
                    writer.Close();
                    writer.Dispose();
                    a.Dispose();
                    request.SendData(stream.ToArray(), "application/zip", 200, true, new Dictionary<string, string> { { "Content-Disposition", "inline; filename=\"OculusDB_CoreMods.qmod\"" } });
				} else
                {
                    request.SendString("", "text/plain", 404);
                }
				return true;
            }), true, true, true, true, 300); // 5 mins
            server.AddRoute("GET", "/api/coremodsproxy", new Func<ServerRequest, bool>(request =>
            {
                WebClient webClient = new WebClient();
                try
                {
                    string res = webClient.DownloadString("https://git.bmbf.dev/unicorns/resources/-/raw/master/com.beatgames.beatsaber/core-mods.json");
                    if (res.Length <= 2) throw new Exception("lol fuck you idiot");
                    request.SendString(res, "application/json", 200, true, new Dictionary<string, string>
                    {
                        {
                            "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                        }
                    });
                    File.WriteAllText(OculusDBEnvironment.dataDir + "coremods.json", res);
                }
                catch (Exception e)
                {
                    if(File.Exists(OculusDBEnvironment.dataDir + "coremods.json"))
                    {
                        if(!request.closed) request.SendString(File.ReadAllText(OculusDBEnvironment.dataDir + "coremods.json"), "application/json", 200, true, new Dictionary<string, string>
                        {
                            {
                                "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                            }
                        });
                    } else
                    {
                        if(!request.closed) request.SendString("{}", "application/json", 500, true, new Dictionary<string, string>
                        {
                            {
                                "access-control-allow-origin", request.context.Request.Headers.Get("origin")
                            }
                        });
                    }
                }

                return true;
            }));
            server.AddRoute("POST", "/api/v1/checkaccess", new Func<ServerRequest, bool>(request =>
            {
                request.SendString((config.accesscode == request.bodyString).ToString().ToLower());
                return true;
            }));
			server.AddRoute("POST", "/api/v1/qavsreport", new Func<ServerRequest, bool>(request =>
			{
                QAVSReport report = JsonSerializer.Deserialize<QAVSReport>(request.bodyString);
                request.SendString(MongoDBInteractor.AddQAVSReport(report));
				return true;
			}));
			server.AddRoute("GET", "/api/v1/qavsreport/", new Func<ServerRequest, bool>(request =>
			{
				request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetQAVSReport(request.pathDiff.ToUpper())), "application/json");
				return true;
			}), true);
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
                request.SendStringReplace(File.ReadAllText(frontend + "admin.html"), "text/html", 200, replace);
                return true;
            }), true, true, true);
            server.AddRouteFile("/login", frontend + "login.html", replace, true, true, true, 0, true);
            server.AddRouteFile("/style.css", frontend + "style.css", replace, true, true, true, 0, true);
            server.AddRoute("POST", "/api/updateserver/", new Func<ServerRequest, bool>(request =>
            {
                if (!IsUserAdmin(request)) return true;
                request.SendString("This endpoint has been deprecated. Please use MasterServer for deploying updates.");
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
                    List<DBApplication> results = new ();
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
                    request.SendString(JsonSerializer.Serialize(results), "application/json");
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
                string file = frontend + "applicationspecific" + Path.DirectorySeparatorChar + request.pathDiff + ".html";
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
                string currency = request.queryString.Get("currency") ?? "";
                List<DBApplication> apps = MongoDBInteractor.GetAllApplications(currency);
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
				/*
				 * Check if app exists before adding it, however that's not needed as OculusDB will just skip it if it doesn't find it later.
                Data<Application> app = GraphQLClient.GetAppDetail(appId, h);

                if(app.data.node == null)
                {
                    request.SendString("This app couldn't be found on oculus. Make sure you typed an app ID and NOT an app name", "text/plain", 400);
                    return true;
				}
                */
				if (!Regex.IsMatch(appId, "[0-9]+"))
				{
					request.SendString("This link or id cannot be processed. Make sure you actually input a correct link or id. App names will NOT work", "text/plain", 400);
					return true;
				}

                AppToScrape s = new AppToScrape
                {
                    appId = appId,
                    headset = h,
                    scrapePriority = AppScrapePriority.High,
                    priority = false
                };
				ScrapingNodeMongoDBManager.AddApp(s);

				request.SendString("The app has been queued to get added. Allow us up to 5 hours to add the app. Thanks for your collaboration");
				return true;
			}), true, true, true, true);
			
			server.AddRoute("GET", "/api/v1/packagename/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    List<DBApplication> d = MongoDBInteractor.GetApplicationByPackageName(request.pathDiff);
                    if (d.Count <= 0)
                    {
                        request.SendString("{}", "application/json", 404);
                        return true;
                    }
                    request.SendString(JsonSerializer.Serialize(d.First()), "application/json");
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
                    List<BsonDocument> d = MongoDBInteractor.GetByID(request.pathDiff, 1, request.queryString.Get("currency") ?? "");
                    if (d.Count <= 0)
					{
						request.SendString("{}", "application/json", 404);
                        if(request.queryString.Get("noscrape") == null)
						{
							Headset h = HeadsetTools.GetHeadsetFromOculusLink(request.pathDiff, Headset.HOLLYWOOD);
                            AppToScrape s = new AppToScrape
                            {
                                appId = request.pathDiff,
                                headset = h,
                                priority = true
                            };
                            ScrapingNodeMongoDBManager.AddApp(s);
                        }
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
                    ConnectedList connected = MongoDBInteractor.GetConnected(request.pathDiff, request.queryString.Get("currency") ?? "");
                    request.SendString(JsonSerializer.Serialize(connected), "application/json");

                    // Requests a priority scrape for every app
                    foreach (DBApplication a in connected.applications)
                    {
                        AppToScrape s = new AppToScrape
                        {
                            appId = a.id,
                            headset = a.hmd,
                            priority = true
                        };
                        ScrapingNodeMongoDBManager.AddApp(s);
                    }
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), true, true, true, true, 120); // 2 mins
            server.AddRoute("GET", "/api/v1/versions/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                try
                {
                    List<DBVersion> versions = MongoDBInteractor.GetVersions(request.pathDiff, request.queryString.Get("onlydownloadable") != null && request.queryString.Get("onlydownloadable").ToLower() != "false");
                    request.SendString(JsonSerializer.Serialize(versions), "application/json");

                    if (versions.Count > 0)
                    {
                        DBApplication a = ObjectConverter.ConvertToDBType(MongoDBInteractor.GetByID(versions[0].parentApplication.id)[0]);
                        AppToScrape s = new AppToScrape
                        {
                            appId = a.id,
                            headset = a.hmd,
                            priority = true
                        };
                        ScrapingNodeMongoDBManager.AddApp(s);
                    }
                }
                catch (Exception e)
                {
                    request.SendString(apiError, "text/plain", 500);
                    Logger.Log(e.ToString(), LoggingType.Error);
                }
                return true;
            }), true, true, true, true, 120); // 2 mins
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
                    List<DBApplication> d = MongoDBInteractor.SearchApplication(HttpUtility.UrlDecode(request.pathDiff), headsets, request.queryString.Get("quick") == null ? false : true);
                    if (d.Count <= 0)
                    {
                        request.SendString("[]", "application/json", 200);
                        return true;
                    }
                    request.SendString(JsonSerializer.Serialize(d), "application/json");
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
                    string currency = request.queryString.Get("currency") ?? "";
                    foreach (BsonDocument d in MongoDBInteractor.GetPriceChanges(request.pathDiff, currency)) changes.Add(ObjectConverter.ConvertToDBType(d));
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
				string application = "";
                string currency = "";
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
					application = request.queryString.Get("application") ?? "";
                    currency = request.queryString.Get("currency") ?? "";
				}
                catch (Exception ex)
                {
                    Logger.Log(ex.ToString(), LoggingType.Warning);
                    request.SendString("count and skip must be numerical values", "text/plain", 400);
                }
                try
                {
                    List<BsonDocument> activities = MongoDBInteractor.GetLatestActivities(count, skip, typeConstraint, application, currency);
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
                    info.dataDocuments = MongoDBInteractor.CountDataDocuments();
                    info.activityDocuments = MongoDBInteractor.CountActivityDocuments();
                    info.scrapingStatusPageUrl = config.scrapingMasterUrl;
                    info.appCount = MongoDBInteractor.GetAppCount();
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

                DBAppImage img = MongoDBInteractor.GetAppImage(request.pathDiff);
                if(img == null) request.Send404();
                else request.SendData(img.data, img.mimeType);
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

            server.AddRouteFile("/", frontend + "home.html", replace, true, true, true, accessCheck);
			server.AddRouteFile("/alias", frontend + "alias.html", replace, true, true, true, accessCheck);
			server.AddRouteFile("/recentactivity", frontend + "recentactivity.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/server", frontend + "server.html", replace, true, true, true, accessCheck);
            
            server.AddRouteFile("/downloadstats", frontend + "downloadstats.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/search", frontend + "search.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/logo", frontend + "logo.png", true, true, true, accessCheck);
            server.AddRouteFile("/notfound.jpg", frontend + "notfound.jpg", true, true, true, accessCheck);
            server.AddRouteFile("/favicon.ico", frontend + "favicon.png", true, true, true, accessCheck);
            server.AddRouteFile("/privacy", frontend + "privacy.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/saved", frontend + "saved.html", replace, true, true, true, accessCheck);
            
            server.AddRoute("GET", "/console", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                if (!IsUserAdmin(request)) return true;
                request.SendStringReplace(File.ReadAllText(frontend + "console.html"), "text/html", 200, replace);
                return true;
            }), true, true, true);
            server.AddRoute("GET", "/id/", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                request.SendStringReplace(File.ReadAllText(frontend + "id.html").Replace("{0}", request.pathDiff), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRoute("GET", "/activity", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                request.SendStringReplace(File.ReadAllText(frontend + "activity.html").Replace("{0}", request.pathDiff), "text/html", 200, replace);
                return true;
            }), true, true, true, true);
            server.AddRouteFile("/explore", frontend + "explore.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/script.js", frontend + "script.js", replace, true, true, true, accessCheck);
            server.AddRouteFile("/api/docs", frontend + "api.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/jsonview.js", frontend + "jsonview.js", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide", frontend + "guide.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/supportus", frontend + "supportus.html", replace, true, true, true, accessCheck);
			server.AddRouteFile("/qavslogs", frontend + "qavsloganalyser.html", replace, true, true, true, accessCheck);

			// for all the annoying people out there4
			server.AddRouteRedirect("GET", "/idiot", "/guide/quest");

            server.AddRouteFile("/guide/quest", frontend + "guidequest.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide/quest/pc", frontend + "guidequest_PC.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide/quest/qavs", frontend + "guidequest_QAVS.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/guide/quest/sqq", frontend + "guidequest_SQQ.html", replace, true, true, true, accessCheck);
            server.AddRouteFile("/assets/sq.png", frontend + "sq.png", true, true, true, accessCheck);
            server.AddRouteFile("/assets/discord.svg", frontend + "discord.svg", true, true, true, accessCheck);


            server.AddRouteFile("/guide/rift", frontend + "guiderift.html", replace, true, true, true, accessCheck);
            server.AddRoute("GET", "/api/api.json", new Func<ServerRequest, bool>(request =>
            {
                if (!DoesUserHaveAccess(request)) return true;
                request.SendString(File.ReadAllText(frontend + "api.json").Replace("\n", ""), "application/json", 200);
                return true;
            }), true, true, true, true);

            ///////////////////// BLOCK OCULUSDB HERE
            if (isBlocked)
            {
                // Block entire OculusDB
                server.AddRoute("GET", "/blocked", new Func<ServerRequest, bool>(request =>
                {
                    request.SendFile(frontend + "blocked.html", replace);
                    return true;
                }), true, true, true, true);
                //return;
            }


            //// jokes are fun
            server.AddRouteFile("/cdn/boom.ogg", frontend + "assets" + Path.DirectorySeparatorChar + "boom.ogg", true, true, true, accessCheck);
            server.AddRouteFile("/cdn/modem.ogg", frontend + "assets" + Path.DirectorySeparatorChar + "modem.ogg", true, true, true, accessCheck);

            server.AddRouteFile("/cdn/BS2.jpg", frontend + "assets" + Path.DirectorySeparatorChar + "BS2.jpg", true, true, true, accessCheck);
        }

        private bool DoesTokenHaveAccess(ServerRequest request, Permission p)
        {
            string token = request.queryString.Get("token");
            if (token != null)
            {
                for (int i = 0; i < config.tokens.Count; i++)
                {
                    if(config.tokens[i].expiry < DateTime.UtcNow)
                    {
                        request.SendString("Token expired");
                        return false;
                    }
                    if(config.tokens[i].token == token)
                    {
                        if (config.tokens[i].permissions.Contains(p)) return true;
                        else
                        {
                                
                            request.SendString("No permission to perform " + p);
                            return false;
                        }
                    }
                }
            }
            request.Send403();
            return false;
        }
}