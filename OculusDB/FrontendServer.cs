using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
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
using OculusDB.Api;
using OculusDB.ApiDocs;
using OculusDB.Database;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusDB.QAVS;
using OculusDB.ScrapingMaster;
using OculusDB.ScrapingNodeCode;
using OculusDB.Search;
using OculusDB.Users;
using OculusGraphQLApiLib;

namespace OculusDB;

public class FrontendServer
{
    public HttpServer server = null;
    public static string frontend = "";
    public static Config config { get { return OculusDBEnvironment.config; } set { OculusDBEnvironment.config = value; } }
    public static bool isBlocked = false;
    public static Dictionary<string, string> replace = new Dictionary<string, string>
    {
        {"{meta}", "<meta charset=\"UTF-8\">\n<meta name=\"theme-color\" content=\"#63fac3\">\n<meta name=\"site_name\" content=\"OculusDB\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">" },
        {"{oculusloginlink}", "https://developer.oculus.com/manage/" },
        {"{BSLGDC}", "https://discord.gg/MrwMx5e" },
        {"{OculusDBDC}", "https://discord.gg/zwRfHQN2UY" },
        {"{headsetjson}", JsonSerializer.Serialize(HeadsetIndex.entries)}
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
		OculusDBDatabase.Initialize();
        ScrapingNodeMongoDBManager.Init();

        Logger.Log("Setting up routes");
        frontend = OculusDBEnvironment.debugging ? @"../../../frontend/" : "frontend" + Path.DirectorySeparatorChar;
        
        ////////////////// Admin
        server.AddRouteRedirect("GET", "/api/config", "/api/v2/admin/config");
        server.AddRoute("GET", "/api/v2/admin/config", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            if (!IsUserAdmin(request)) return true;
            request.SendString(JsonSerializer.Serialize(config));
            return true;
        }));
        
        server.AddRouteRedirect("POST", "/api/config", "/api/v2/admin/config");
        server.AddRoute("POST", "/api/v2/admin/config", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            if (!IsUserAdmin(request)) return true;
            config = JsonSerializer.Deserialize<Config>(request.bodyString);
            config.Save();
            request.SendString("Updated config");
            return true;
        }));
        server.AddRouteRedirect("GET", "/api/v1/admin/users", "/api/v2/admin/users");
        server.AddRoute("GET", "/api/v2/admin/users", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            if (!IsUserAdmin(request)) return true;
            request.SendString(JsonSerializer.Serialize(config.tokens));
            return true;
        }));
        server.AddRouteRedirect("POST", "/api/v1/admin/users", "/api/v2/admin/users");
        server.AddRoute("POST", "/api/v2/admin/users", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            if (!IsUserAdmin(request)) return true;
            config.tokens = JsonSerializer.Deserialize<List<Token>>(request.bodyString);
            request.SendString("Set users");
            return true;
        }));
        
        /////// Blocked
        server.AddRouteRedirect("GET", "/api/v1/blocked/blockedapps", "/api/v2/blocked/blockedapps");
        server.AddRoute("GET", "/api/v2/blocked/blockedapps", request =>
        {
            request.SendString(JsonSerializer.Serialize(OculusDBDatabase.GetBlockedApps()), "application/json");
            return true;
        });
        server.AddRouteRedirect("DELETE", "/api/v1/blocked/unblockapp", "/api/v2/blocked/unblockapp");
        server.AddRoute("DELETE", "/api/v2/blocked/unblockapp", request =>
        {
            string id = request.queryString.Get("id");
            if (!DoesTokenHaveAccess(request, Permission.BlockApps))
            {
                return true;
            }
            OculusDBDatabase.UnblockApp(id);
            request.SendString("unblocked " + id, "application/json");
            return true;
        });
        server.AddRouteRedirect("POST", "/api/v1/blocked/blockapp", "/api/v2/blocked/blockapp");
        server.AddRoute("POST", "/api/v2/blocked/blockapp", request =>
        {
            string id = request.queryString.Get("id") ?? "";
            if (!DoesTokenHaveAccess(request, Permission.BlockApps))
            {
                return true;
            }
            if(id == "")
            {
                request.SendString("id must be supplied", "text/plain", 400);
                return true;
            }
            OculusDBDatabase.BlockApp(id);
            request.SendString("blocked " + id, "application/json");
            return true;
        });
        
        ////////// Scraping
        server.AddRouteRedirect("POST", "/api/v1/createscrapingnode", "/api/v2/scraping/createnode");
        server.AddRoute("POST", "/api/v2/scraping/createnode", request =>
        {
            string id = request.queryString.Get("id") ?? "";
            string name = request.queryString.Get("name") ?? "";
            if (!DoesTokenHaveAccess(request, Permission.CreateScrapingNode))
            {
                return true;
            }
            if(id == "" || name == "")
            {
                request.SendString("id and name must be supplied", "text/plain", 400);
                return true;
            }
            request.SendString(ScrapingNodeMongoDBManager.CreateScrapingNode(id, name), "application/json");
            return true;
        });
        server.AddRouteRedirect("POST", "/api/v1/admin/scrape/", "/api/v2/scraping/scrape");
        server.AddRoute("POST", "/api/v2/scraping/scrape", request =>
        {
            AppToScrape s = JsonSerializer.Deserialize<AppToScrape>(request.bodyString);
            if (!DoesTokenHaveAccess(request, s.priority ? Permission.StartPriorityScrapes : Permission.StartScrapes))
            {
                return true;
            }
            try
            {
                // Create scraping node for scraping, start that node and supply one task to it
                ScrapingNodeMongoDBManager.AddAppToScrape(s, AppScrapePriority.High);
                request.SendString("Added scrape to queue as first to be scraped. No idea if it'll scrape or not but a scraping node will defo try.");
            }
            catch (Exception e)
            {
                request.SendString(e.ToString(), "text/plain", 500);
            }
            return true;
        });
        server.AddRouteRedirect("GET", "/api/v1/reportmissing/", "/api/v2/scraping/reportmissing/", true);
        server.AddRoute("GET", "/api/v2/scraping/reportmissing/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            string appId = request.pathDiff.Split('?')[0];
            if (appId.EndsWith("/")) appId = appId.TrimEnd('/');
            if(appId.LastIndexOf("/") >= 0) appId = appId.Substring(appId.LastIndexOf("/") + 1);

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
                scrapePriority = AppScrapePriority.High,
                priority = false
            };
            ScrapingNodeMongoDBManager.AddAppToScrape(s);

            request.SendString("The app has been queued to get added. Allow us up to 5 hours to add the app. Thanks for your collaboration");
            return true;
        }), true, true, true, true);


        ////////////////// Aliases
        server.AddRouteRedirect("GET", "/api/v1/aliases/applications", "/api/v2/aliases/applications");
        server.AddRoute("GET", "/api/v2/aliases/applications", new Func<ServerRequest, bool>(request =>
        {
            request.SendString(JsonSerializer.Serialize(VersionAlias.GetApplicationsWithAliases()));
            return true;
        }));
        server.AddRouteRedirect("POST", "/api/v1/aliases/", "/api/v2/aliases/add");
		server.AddRoute("POST", "/api/v2/aliases/add", new Func<ServerRequest, bool>(request =>
		{
            if (!IsUserAdmin(request)) return true;
            List<VersionAlias> aliases = JsonSerializer.Deserialize<List<VersionAlias>>(request.bodyString);
            foreach(VersionAlias a in aliases)
            {
                VersionAlias.AddVersionAlias(a);
            }
            request.SendString("Added aliases");
			return true;
		}));

		////////////////// Login
		server.AddRouteRedirect("POST", "/api/v1/login", "/api/v2/login");
		server.AddRoute("POST", "/api/v2/login", new Func<ServerRequest, bool>(request =>
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
        ////////// Game specific api
        BeatSaberApi.SetupRoutes(server);
        ////////// Emergency
        server.AddRouteRedirect("POST", "/api/v1/checkaccess","/api/v2/emergency/checkaccess");
        server.AddRoute("POST", "/api/v2/emergency/checkaccess", new Func<ServerRequest, bool>(request =>
        {
            request.SendString((config.accesscode == request.bodyString).ToString().ToLower());
            return true;
        }));
        ///////// QAVS
        server.AddRouteRedirect("POST", "/api/v1/qavsreport","/api/v2/qavs/createreport");
		server.AddRoute("POST", "/api/v2/qavs/createreport", new Func<ServerRequest, bool>(request =>
		{
            QAVSReport report = JsonSerializer.Deserialize<QAVSReport>(request.bodyString);
            request.SendString(QAVSReport.AddQAVSReport(report));
			return true;
		}));
        server.AddRouteRedirect("GET", "/api/v1/qavsreport/","/api/v2/qavs/report/", true);
		server.AddRoute("GET", "/api/v2/qavs/report/", new Func<ServerRequest, bool>(request =>
		{
			request.SendString(JsonSerializer.Serialize(QAVSReport.GetQAVSReport(request.pathDiff.ToUpper())), "application/json");
			return true;
		}), true);
		
        ///////////// Analytics
        server.AddRouteRedirect("POST", "/api/v1/reportdownload", "/api/v2/analytics/reportdownload");
        server.AddRoute("POST", "/api/v2/analytics/reportdownload", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            request.SendString(JsonSerializer.Serialize(AnalyticManager.ProcessAnalyticsRequest(request)));
            return true;
        }));
        server.AddRouteRedirect("GET", "/api/v1/applicationanalytics/", "/api/v2/analytics/applicationanalytics/", true);
        server.AddRoute("GET", "/api/v2/analytics/applicationanalytics/", new Func<ServerRequest, bool>(request =>
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
        
        
        ///// WIP
        AddDeprecatedRoute("GET", "/api/v1/explore", true);
        server.AddRoute("GET", "/api/v2/wip/explore", new Func<ServerRequest, bool>(request =>
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
                request.SendString(JsonSerializer.Serialize(results), "application/json");
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }));
        
        //////// Application specific html
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
        
        ///////// all apps
        AddDeprecatedRoute("GET", "/api/v1/allapps", true);
        server.AddRoute("GET", "/api/v2/allapps", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            List<DBApplication> apps = OculusDBDatabase.GetAllApplications();
            request.SendString(JsonSerializer.Serialize(apps), "application/json");
            return true;
        }), false, true, true, true);
        ///////// Package name
        AddDeprecatedRoute("GET", "/api/v1/packagename", true);
		server.AddRoute("GET", "/api/v2/packagename/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                DBApplication? d = DBApplication.ByPackageName(request.pathDiff);
                if (d == null)
                {
                    request.SendString("{}", "application/json", 404);
                    return true;
                }
                request.SendString(JsonSerializer.Serialize(d), "application/json");
            } catch(Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }), true, true, true, true);
        //////// id
        AddDeprecatedRoute("GET", "/api/v1/id", true);
        server.AddRoute("GET", "/api/v2/id/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                DBBase? d = OculusDBDatabase.GetDocument(request.pathDiff);
                if (d == null)
				{
					request.SendString("{}", "application/json", 404);
                    if(request.queryString.Get("noscrape") == null && new Regex(@"^[0-9]+$").IsMatch(request.pathDiff))
					{
                        AppToScrape s = new AppToScrape
                        {
                            appId = request.pathDiff,
                            priority = true
                        };
                        ScrapingNodeMongoDBManager.AddAppToScrape(s);
                    }
                    return true;
				}
				request.SendString(JsonSerializer.Serialize(ObjectConverter.ConvertToDBType(d)), "application/json");
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            
            return true;
        }), true, true, true, true, 120); // 2 mins
        /////// Connected
        AddDeprecatedRoute("GET", "/api/v1/connected", true);
        server.AddRoute("GET", "/api/v2/connected/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                ConnectedList? connected = MongoDBInteractor.GetConnected(request.pathDiff);
                if (connected == null)
                {
                    request.SendString(JsonSerializer.Serialize(new ConnectedList()), "application/json", 404);
                    return true;
                }
                request.SendString(JsonSerializer.Serialize(connected), "application/json");

                // Requests a priority scrape for every app
                foreach (DBApplication a in connected.applications)
                {
                    AppToScrape s = new AppToScrape
                    {
                        appId = a.id,
                        priority = true
                    };
                    ScrapingNodeMongoDBManager.AddAppToScrape(s);
                }
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }), true, true, true, true, 120); // 2 mins
        /////// Versions
        AddDeprecatedRoute("GET", "/api/v1/versions", true);
        server.AddRoute("GET", "/api/v2/versions/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                List<DBVersion> versions = MongoDBInteractor.GetVersions(request.pathDiff, request.queryString.Get("onlydownloadable") != null && request.queryString.Get("onlydownloadable").ToLower() != "false");
                request.SendString(JsonSerializer.Serialize(versions), "application/json");

                if (versions.Count > 0 && versions[0].parentApplication != null)
                {
                    
                    AppToScrape s = new AppToScrape
                    {
                        appId = versions[0].parentApplication?.id ?? "",
                        priority = true
                    };
                    ScrapingNodeMongoDBManager.AddAppToScrape(s);
                }
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }), true, true, true, true, 120); // 2 mins
        AddDeprecatedRoute("GET", "/api/v1/search", true);
        server.AddRoute("GET", "/api/v2/search/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                request.SendString(JsonSerializer.Serialize(SearchQueryExecutor.ExecuteQuery(SearchQuery.FromRequest(request))), "application/json");
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }), true, true, true, true, 360); // 6 mins
        AddDeprecatedRoute("GET", "/api/v1/dlcs", true);
        server.AddRoute("GET", "/api/v2/dlcs/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                MongoDBInteractor.GetDlcs(request.pathDiff);
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }), true, true, true, true, 360); // 6 mins
        /////// Difference
        AddDeprecatedRoute("GET", "/api/v1/pricehistory", true);
        server.AddRoute("GET", "/api/v2/difference/pricehistory/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                request.SendString(JsonSerializer.Serialize(MongoDBInteractor.GetFormerPricesOfId(request.pathDiff)), "application/json");
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }), true, true, true, true, 360); // 6 mins
        AddDeprecatedRoute("GET", "/api/v1/activity", true);
        AddDeprecatedRoute("GET", "/api/v1/activityid/", true);
        server.AddRoute("GET", "/api/v2/difference/id/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
                DBDifference? d = DBDifference.ById(request.pathDiff);
                if (d == null)
                {
                    request.SendString("{}", "application/json", 404);
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
        }), true, true, true, true);
        server.AddRouteRedirect("GET", "/api/v1/updates", "/api/v2/commits");
        server.AddRoute("GET", "/api/v2/commits", new Func<ServerRequest, bool>(request =>
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
        ////// Database
        AddDeprecatedRoute("GET", "/api/v1/database", false);
        server.AddRoute("GET", "/api/v2/database/info", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            try
            {
				request.SendString(JsonSerializer.Serialize(OculusDBDatabase.GetDbInfo()));
            }
            catch (Exception e)
            {
                request.SendString(apiError, "text/plain", 500);
                Logger.Log(e.ToString(), LoggingType.Error);
            }
            return true;
        }));
        /// application images
        server.AddRoute("GET", "/assets/app/", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            if (!(new Regex(@"^[0-9]+$").IsMatch(request.pathDiff)))
            {
                request.SendString("Only application ids are allowed", "text/plain", 400);
                return true;
            }

            DBAppImage? img = DBAppImage.ById(request.pathDiff);
            if(img == null) request.Send404();
            else request.SendData(img.data, img.mimeType);
            return true;
        }), true, true, true, true, 1800, true); // 30 mins
        ///////////// Lists
        server.AddRouteRedirect("GET", "/api/v1/headsets", "/api/v2/lists/headsets");
        server.AddRoute("GET", "/api/v2/lists/headsets", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            request.SendString(JsonSerializer.Serialize(HeadsetIndex.entries));
            return true;
        }));
        server.AddRoute("GET", "/api/v2/lists/differencetypes", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            request.SendString(JsonSerializer.Serialize(EnumIndex.differenceNameTypes));
            return true;
        }));
        server.AddRoute("GET", "/api/v2/lists/searchcategories", new Func<ServerRequest, bool>(request =>
        {
            if (!DoesUserHaveAccess(request)) return true;
            request.SendString(JsonSerializer.Serialize(EnumIndex.searchEntryTypes));
            return true;
        }));
        ////////////// ACCESS CHECK IF OCULUSDB IS BLOCKED
        Func<ServerRequest, bool> accessCheck = null;
        /*
        new Func<ServerRequest, bool>(request =>
        {
            return DoesUserHaveAccess(request);
        });
        */
        byte[] indexHtml = File.ReadAllBytes(frontend + "index.html");
        /// Redirect all other queries to index.html
        server.notFoundHandler = request =>
        {
            string path = request.path.ToLower();
            if (path.StartsWith("/")) path = path.Substring(1);
            if (path.StartsWith("cdn") || path.StartsWith("api")) return false;
            request.SendData(indexHtml, "text/html");
            return true;
        };
        foreach (string file in Directory.GetFiles(frontend + "assets"))
        {
            server.AddRouteFile("/assets/" + Path.GetFileName(file), file, replace, true, true, true, accessCheck);
        }
        
        server.AddRouteFile("/favicon.ico", frontend + "favicon.png", true, true, true, accessCheck);
		server.AddRouteFile("/qavslogs", frontend + "qavsloganalyser.html", replace, true, true, true, accessCheck);

        server.AddRoute("GET", "/fonts/OpenSans", request =>
        {
            ProxyChangeFontUrl("https://fonts.googleapis.com/css?family=Open+Sans:400,400italic,700,700italic", request);
            return true;
        }, false, true, true, true, 3600, true, 0);
        server.AddRoute("GET", "/cdn/flag.svg", request =>
        {
            Proxy("https://upload.wikimedia.org/wikipedia/commons/f/fd/LGBTQ%2B_rainbow_flag_Quasar_%22Progress%22_variant.svg", request);
            return true;
        }, false, true, true, true, 3600, true, 0);
        server.AddRoute("GET", "/proxy", request =>
        {
            Proxy(request.queryString.Get("url"), request);
            return true;
        }, false, true, true, true, 3600, true, 0);
        
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
        
        
        Logger.Log("Updating API Docs");
        ApiDocsCreator.UpdateApiDocs();


        //// jokes are fun
        server.AddRouteFile("/cdn/boom.ogg", frontend + "assets" + Path.DirectorySeparatorChar + "boom.ogg", true, true, true, accessCheck);
        server.AddRouteFile("/cdn/modem.ogg", frontend + "assets" + Path.DirectorySeparatorChar + "modem.ogg", true, true, true, accessCheck);

        server.AddRouteFile("/cdn/BS2.jpg", frontend + "assets" + Path.DirectorySeparatorChar + "BS2.jpg", true, true, true, accessCheck);
    }

    private void AddDeprecatedRoute(string method, string path, bool ignoreEnd)
    {
        server.AddRoute(method, path, request =>
        {
            request.SendString("This endpoint is deprecated and does not work anymore. Please refer to the api docs for updated endpoints. /api/docs", "text/plain", 510);
            return true;
        }, ignoreEnd);
    }

    private void ProxyChangeFontUrl(string url, ServerRequest request)
    {
        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
        webRequest.Method = "GET";
        
        // Send get request
        HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
        MemoryStream ms = new MemoryStream();
        webResponse.GetResponseStream().CopyTo(ms);
        byte[] cssData = ms.ToArray();
        string cssString = Encoding.Default.GetString(cssData);
        Logger.Log(cssString);
        Regex r = new Regex(@"url([^)]*)");
        foreach (Match match in r.Matches(cssString))
        {
            cssString = cssString.Replace(match.Value, "url(/proxy?url=" + match.Value.Substring(4));
        }
        request.SendString(cssString, webResponse.ContentType);
        webResponse.Close();
    }

    private void Proxy(string url, ServerRequest request)
    {
        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
        webRequest.Method = "GET";
        
        // Send get request
        HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
        MemoryStream ms = new MemoryStream();
        webResponse.GetResponseStream().CopyTo(ms);
        request.SendData(ms.ToArray(), webResponse.ContentType);
        webResponse.Close();
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