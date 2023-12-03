using System.Text.Json;
using ComputerUtils.Logging;
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
        string appId = "1304877726278670";
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
            l = null,
            h = Headset.RIFT,
            d = DateTime.MaxValue,
            c = new DiffTestChild
            {
                t = "a"
            }
        };
        Logger.Log(JsonSerializer.Serialize(DiffMaker.GetDifference(diffTestA, diffTestB), new JsonSerializerOptions
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
        List<DBIAPItemPack> dlcPacks = new List<DBIAPItemPack>();
        // Get DLC Packs and prices
        // Add DLCs
        List<DBIAPItem> iapItems = new List<DBIAPItem>();
        int i = 0;
        foreach (IAPItem iapItem in OculusInteractor.EnumerateAllDLCs(dbApp.grouping.id))
        {
            Logger.Log(i.ToString());
            i++;
            iapItems.Add(OculusConverter.AddScrapingNodeName(OculusConverter.IAPItem(iapItem, dbApp), scrapingNodeName));
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
        List<VersionAlias> versionAliases = MongoDBInteractor.GetVersionAliases(dbApp.id);
        foreach (OculusBinary binary in OculusInteractor.EnumerateAllVersions(dbApp.id))
        {
            DBVersion v = OculusConverter.AddScrapingNodeName(OculusConverter.Version(GraphQLClient.GetMoreBinaryDetails(binary.id).data.node, applicationFromDeveloper, versionAliases), scrapingNodeName);
            Logger.Log(v.versionCode.ToString());
            versions.Add(v);
        }
        
        
        // Add Achievements
        List<DBAchievement> achievements = new List<DBAchievement>();
        foreach (AchievementDefinition achievement in OculusInteractor.EnumerateAllAchievements(dbApp.id))
        {
            achievements.Add(OculusConverter.AddScrapingNodeName(OculusConverter.Achievement(achievement, dbApp), scrapingNodeName));
        }
        Logger.Log(JsonSerializer.Serialize(achievements));
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