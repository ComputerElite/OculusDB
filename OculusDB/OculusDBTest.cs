using System.Text.Json;
using ComputerUtils.Logging;
using OculusDB.Constants;
using OculusDB.Database;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB;

public class DiffTest
{
    [TrackChanges]
    public string appId { get; set; } = "24162327616747873";
    [TrackChanges]
    public bool log { get; set; } = false;
    [TrackChanges]
    public double t { get; set; } = 0;
    [TrackChanges]
    public List<DiffTestChild> list { get; set; } = new List<DiffTestChild>();
    [TrackChanges]
    public List<string> list2 { get; set; } = new List<string>();
    [TrackChanges]
    public long? l { get; set; } = 0;
    [TrackChanges]
    public Headset h { get; set; } = Headset.INVALID;
    [TrackChanges]
    public DateTime? d { get; set; } = DateTime.Today;
    [TrackChanges]
    public DiffTestChild? c { get; set; } = null;
}

public class DiffTestChild
{
    [TrackChanges]
    public string t { get; set; } = "";
}

public class OculusDBTest
{
    public static void Test()
    {
        
        GraphQLClient.log = false;
        /*
        Console.WriteLine("{");
        foreach (Application application in OculusInteractor.EnumerateAllApplications(Headset.GEARVR))
        {
            Console.Write("\"" + application.id + "\",");
            //
        }
        Console.WriteLine("}");
        return;
        */

        /*
         // Check for custom items
        while (appIds.Count > 0)
        {
            ApplicationGrouping g = GraphQLClient.GetCustomItems(appIds[0]).data.node;
            if (g.worlds_custom_developer_item_definitions.edges.Count > 0) Console.WriteLine(appIds[0]);
            appIds.RemoveAt(0);
            if(appIds.Count % 100 == 0) Console.WriteLine(appIds.Count);
        }
        return;
        */
        List<string> appIds = AppIdList.appIdsGearVR;
        if(!File.Exists("appIds.json")) File.WriteAllText("appIds.json", JsonSerializer.Serialize(appIds));
        appIds = JsonSerializer.Deserialize<List<string>>(File.ReadAllText("appIds.json"));
        Console.Write(appIds.Count);
        while (appIds.Count > 0)
        {
            Scrape(appIds[0]);
            appIds.RemoveAt(0);
            File.WriteAllText("appIds.json", JsonSerializer.Serialize(appIds));
        }

        return;
        /*
        Console.WriteLine("{");
        foreach (Application application in OculusInteractor.EnumerateAllApplications(Headset.HOLLYWOOD))
        {
            Console.Write("\"" + application.id + "\",");
            //
        }
        
        Console.WriteLine("}");
        */
        DiffTest diffTestA = new DiffTest
        {
            appId = "",
            log = true,
            t = 0.5,
            list = new List<DiffTestChild>
            {
                new DiffTestChild
                {
                    t = "b"
                }
            },
            list2 = new List<string> {"1", "2"},
            l = 1,
            h = Headset.RIFT,
            d = DateTime.MinValue,
            c = new DiffTestChild
            {
                t = "c"
            }
        };
        DiffTest diffTestB = new DiffTest
        {
            appId = "test",
            log = false,
            t = 0.5,
            list = new List<DiffTestChild>
            {
                new DiffTestChild
                {
                    t = "a"
                }
            },
            list2 = new List<string> {"1", "2", "3"},
            l = null,
            h = Headset.RIFT,
            d = DateTime.MaxValue,
            c = new DiffTestChild
            {
                t = "a"
            }
        };
        Console.WriteLine(JsonSerializer.Serialize(DiffMaker.GetDifference(diffTestA, diffTestB), new JsonSerializerOptions
        {
            WriteIndented = true
        }));
        return;
        

        return;
        //Logger.Log(JsonSerializer.Serialize(OculusConverter.AddScrapingNodeName(dbApp, "Fuck Oculus")));
        //DBVersion version = OculusConverter.Version(GraphQLClient.GetMoreBinaryDetails("24162327616747873").data.node, app, new List<VersionAlias>());
        //Logger.Log(JsonSerializer.Serialize(version));
    }

    public static void Scrape(string appId)
    {
        string scrapingNodeName = "Fuck Oculus node";
        // Add application
        Application applicationFromDeveloper = GraphQLClient.AppDetailsDeveloperAll(appId).data.node;
        Application applicationFromStore = GraphQLClient.AppDetailsMetaStore(appId).data.item;
        
        DBApplication dbApp = OculusConverter.AddScrapingNodeName(OculusConverter.Application(applicationFromDeveloper, applicationFromStore), scrapingNodeName);

        List<DBOffer> offers = new List<DBOffer>();
        offers.Add(OculusConverter.Price(applicationFromStore.current_offer, dbApp));
        List<DBIAPItemPack> dlcPacks = new List<DBIAPItemPack>();
        // Get DLC Packs and prices
        // Add DLCs
        List<DBIAPItem> iapItems = new List<DBIAPItem>();
        int i = 0;
        if (dbApp.grouping != null)
        {
            foreach (IAPItem iapItem in OculusInteractor.EnumerateAllDLCs(dbApp.grouping.id))
            {
                Logger.Log(i.ToString());
                i++;
                iapItems.Add(OculusConverter.AddScrapingNodeName(OculusConverter.IAPItem(iapItem, dbApp), scrapingNodeName));
            }
        }
        else
        {
            dbApp.errors.Add(new DBError
            {
                type = DBErrorType.CouldNotScrapeIaps,
                reason = dbApp.grouping == null ? DBErrorReason.GroupingNull : DBErrorReason.Unknown,
                message = "Couldn't scrape DLCs because grouping is null"
            });
        }
        Data<Application> dlcApplication = GraphQLClient.GetDLCs(dbApp.id);
        if (dlcApplication.data.node != null && dlcApplication.data.node.latest_supported_binary != null && dlcApplication.data.node.latest_supported_binary.firstIapItems != null)
        {
            foreach (Node<AppItemBundle> dlc in dlcApplication.data.node.latest_supported_binary.firstIapItems.edges)
            {
                if (dlc.node.typename_enum == OculusTypeName.IAPItem)
                {
                    // dlc, extract price and add short description
                    if (iapItems.Any(x => x.id == dlc.node.id))
                    {
                        iapItems.FirstOrDefault(x => x.id == dlc.node.id).displayShortDescription = dlc.node.display_short_description;
                    }
                    else
                    {
                        dbApp.errors.Add(new DBError
                        {
                            type = DBErrorType.StoreDlcsNotFoundInExistingDlcs,
                            reason = DBErrorReason.DlcNotInDlcList,
                            message = "DLC " + dlc.node.display_name + " (" + dlc.node.id + ") not found in store existing DLCs"
                        });
                    }
                    offers.Add(
                        OculusConverter.AddScrapingNodeName(OculusConverter.Price(dlc.node.current_offer, dbApp),
                            scrapingNodeName));
                }
                else
                {
                    offers.Add(
                        OculusConverter.AddScrapingNodeName(OculusConverter.Price(dlc.node.current_offer, dbApp),
                            scrapingNodeName));
                    dlcPacks.Add(OculusConverter.AddScrapingNodeName(OculusConverter.IAPItemPack(dlc.node, dbApp.grouping), scrapingNodeName));
                    // dlc pack, extract dlc pack and price
                }
            }
        }
        
        
        // Get Versions
        List<DBVersion> versions = new List<DBVersion>();
        
        foreach (OculusBinary binary in OculusInteractor.EnumerateAllVersions(dbApp.id))
        {
            DBVersion v = OculusConverter.AddScrapingNodeName(OculusConverter.Version(GraphQLClient.GetMoreBinaryDetails(binary.id).data.node, applicationFromDeveloper,dbApp), scrapingNodeName);
            Logger.Log(v.versionCode.ToString());
            versions.Add(v);
        }
        
        

        // Add application packageName
        
        DBVersion? versionToGiveApplication = versions.Count > 0 ? versions[0] : null;
        dbApp.packageName = versionToGiveApplication?.packageName ?? null;
        
        
        // Add Achievements
        List<DBAchievement> achievements = new List<DBAchievement>();
        try
        {
            foreach (AchievementDefinition achievement in OculusInteractor.EnumerateAllAchievements(dbApp.id))
            {
                achievements.Add(OculusConverter.AddScrapingNodeName(OculusConverter.Achievement(achievement, dbApp), scrapingNodeName));
            }
        } catch (Exception e)
        {
            dbApp.errors.Add(new DBError
            {
                type = DBErrorType.CouldNotScrapeAchievements,
                reason = dbApp.grouping == null ? DBErrorReason.GroupingNull : DBErrorReason.Unknown,
                message =e.ToString()
            });
        }
        File.WriteAllText("/home/computerelite/Downloads/full_scrape_" + appId + ".json", JsonSerializer.Serialize(new Dictionary<string, dynamic>
        {
            {"offers", offers},
            {"dlcPacks", dlcPacks},
            {"dlcs", iapItems},
            {"versions", versions},
            {"achievements", achievements},
            {"application", dbApp}
        }));
    }
}