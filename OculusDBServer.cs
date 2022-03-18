using ComputerUtils.Logging;
using ComputerUtils.Updating;
using ComputerUtils.VarUtils;
using ComputerUtils.Webserver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
                    MongoDBInteractor.AddApplication(a);
                    if(MongoDBInteractor.GetLastEventWithIDInDatabase(a.id) == null)
                    {
                        DBActivityNewApplication e = new DBActivityNewApplication();
                        e.id = a.id;
                        e.publisher_name = a.publisher_name;
                        e.releaseDate = TimeConverter.UnixTimeStampToDateTime((long)a.release_date);
                        e.supported_hmd_platforms = a.supported_hmd_platforms;
                        MongoDBInteractor.AddBsonDocumentToActivityCollection(e.ToBsonDocument());
                    }
                    DBActivityPriceChange lastPriceChange = ObjectConverter.ConvertToDBType(MongoDBInteractor.GetLastPriceChangeOfApp(a.id));
                    DBActivityPriceChange priceChange = new DBActivityPriceChange();
                    priceChange.parentApplication.id = a.id;
                    priceChange.parentApplication.canonicalName = a.canonicalName;
                    priceChange.newPriceFormatted = a.current_offer.price.formatted;
                    priceChange.newPriceOffset = a.current_offer.price.offset_amount;
                    if (lastPriceChange != null)
                    {
                        if (lastPriceChange.newPriceOffset == a.current_offer.price.offset_amount) break;
                        priceChange.oldPriceFormatted = lastPriceChange.newPriceFormatted;
                        priceChange.oldPriceOffset = lastPriceChange.newPriceOffset;
                        MongoDBInteractor.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument());
                    } else MongoDBInteractor.AddBsonDocumentToActivityCollection(priceChange.ToBsonDocument());
                    Data<Application> d = GraphQLClient.GetDLCs(a.id);
                    foreach(AndroidBinary b in GraphQLClient.AllVersionsOfApp(a.id).data.node.primary_binaries.nodes)
                    {
                        MongoDBInteractor.AddVersion(b, a);
                        BsonDocument lastActivity = MongoDBInteractor.GetLastEventWithIDInDatabase(b.id);

                        DBActivityNewVersion newVersion = new DBActivityNewVersion();
                        newVersion.id = b.id;
                        newVersion.parentApplication.id = a.id;
                        newVersion.parentApplication.canonicalName = a.canonicalName;
                        newVersion.releaseChannels = b.binary_release_channels.nodes;
                        newVersion.version = b.version;
                        newVersion.versionCode = b.versionCode;
                        newVersion.uploadedTime = TimeConverter.UnixTimeStampToDateTime(b.created_date);
                        if (lastActivity == null)
                        {
                            MongoDBInteractor.AddBsonDocumentToActivityCollection(newVersion.ToBsonDocument());
                        } else
                        {
                            DBActivityVersionUpdated oldUpdate = ObjectConverter.ConvertToDBType(lastActivity);
                            if(String.Join(',', oldUpdate.releaseChannels.Select(x => x.channel_name).ToList()) != String.Join(',', newVersion.releaseChannels.Select(x => x.channel_name).ToList()))
                            {
                                DBActivityVersionUpdated toAdd = ObjectConverter.ConvertCopy<DBActivityVersionUpdated, DBActivityNewVersion>(newVersion);
                                toAdd.__OculusDBType = DBDataTypes.ActivityVersionUpdated;
                            }
                        }
                    }
                    if (d.data.node.latest_supported_binary.firstIapItems == null) continue;
                    Logger.Log("Adding " + d.data.node.latest_supported_binary.firstIapItems.edges.Count + " of " + d.data.node.latest_supported_binary.firstIapItems.count + " DLCs");
                    foreach (Node<AppItemBundle> dlc in d.data.node.latest_supported_binary.firstIapItems.edges)
                    {
                        Logger.Log("Adding dlc " + dlc.node.id);
                        DBActivityNewDLC newDLC = new DBActivityNewDLC();
                        newDLC.id = dlc.node.id;
                        newDLC.parentApplication.id = a.id;
                        newDLC.parentApplication.canonicalName = a.canonicalName;
                        newDLC.displayName = dlc.node.display_name;
                        newDLC.displayShortDescription = dlc.node.display_short_description;
                        BsonDocument oldDLC = MongoDBInteractor.GetLastEventWithIDInDatabase(dlc.node.id);
                        if (dlc.node.IsIAPItem())
                        {
                            MongoDBInteractor.AddDLC(dlc.node);
                            if (oldDLC == null)
                            {
                                MongoDBInteractor.AddBsonDocumentToActivityCollection(newDLC.ToBsonDocument());
                            } else if(oldDLC["displayName"] != newDLC.displayName || oldDLC["displayShortDescription"] != newDLC.displayShortDescription)
                            {
                                DBActivityDLCUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCUpdated, DBActivityNewDLC>(newDLC);
                                updated.__OculusDBType = DBDataTypes.ActivityDLCUpdated;
                                MongoDBInteractor.AddBsonDocumentToActivityCollection(updated.ToBsonDocument().ToBsonDocument().ToBsonDocument().ToBsonDocument());
                            }
                            
                        }
                        else
                        {
                            MongoDBInteractor.AddDLCPack(dlc.node);
                            DBActivityNewDLCPack newDLCPack = ObjectConverter.ConvertCopy<DBActivityNewDLCPack, DBActivityNewDLC>(newDLC);
                            newDLCPack.__OculusDBType = DBDataTypes.ActivityNewDLCPack;
                            foreach(Node<IAPItem> item in dlc.node.bundle_items.edges)
                            {
                                Node<AppItemBundle> matching = d.data.node.latest_supported_binary.firstIapItems.edges.FirstOrDefault(x => x.node.id == item.node.id);
                                if (matching == null) continue;
                                DBActivityNewDLCPackDLC dlcItem = new DBActivityNewDLCPackDLC();
                                dlcItem.id = matching.node.id;
                                dlcItem.displayName = matching.node.display_name;
                                dlcItem.shortDescription = matching.node.display_short_description;
                                newDLCPack.includedDLCs.Add(dlcItem);
                            }
                            if (oldDLC == null)
                            {
                                MongoDBInteractor.AddBsonDocumentToActivityCollection(newDLCPack.ToBsonDocument());
                            }
                            else if (oldDLC["displayName"] != newDLC.displayName || oldDLC["displayShortDescription"] != newDLC.displayShortDescription || String.Join(',', BsonSerializer.Deserialize<DBActivityNewDLCPack>(oldDLC).includedDLCs.Select(x => x.id).ToList()) != String.Join(',', newDLCPack.includedDLCs.Select(x => x.id).ToList()))
                            {
                                DBActivityDLCPackUpdated updated = ObjectConverter.ConvertCopy<DBActivityDLCPackUpdated, DBActivityNewDLCPack>(newDLCPack);
                                updated.__OculusDBType = DBDataTypes.ActivityDLCPackUpdated;
                                MongoDBInteractor.AddBsonDocumentToActivityCollection(updated.ToBsonDocument());
                            }
                        }
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