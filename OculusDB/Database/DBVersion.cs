using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBVersion : DBBase, IDBObjectOperations<DBVersion>
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Version;
    [BsonElement("_lps")]
    public DateTime __lastPriorityScrape { get; set; } = DateTime.MinValue;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("p")]
    public DBParentApplication? parentApplication { get; set; } = null;
    [TrackChanges]
    [BsonElement("bt")]
    public HeadsetBinaryType binaryType { get; set; } = HeadsetBinaryType.Unknown;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [OculusField("version")]
    [TrackChanges]
    [BsonElement("v")]
    public string version { get; set; } = "";
    
    [BsonIgnore]
    public string? alias { get; set; } = "";
    
    [OculusField("versionCode")]
    [TrackChanges]
    [BsonElement("vc")]
    public long versionCode { get; set; } = 0;
    
    [OculusField("changeLog")]
    [TrackChanges]
    [BsonElement("l")]
    public string changelog { get; set; } = "";
    
    [OculusField("created_date_datetime")]
    [TrackChanges]
    [BsonElement("ud")]
    public DateTime uploadedDate { get; set; } = DateTime.MinValue;
    
    [OculusField("size_numerical")]
    [TrackChanges]
    [BsonElement("s")]
    public long size { get; set; } = 0;

    [BsonIgnore]
    public string sizeFormatted
    {
        get
        {
            return SizeConverter.ByteSizeToString(size);
        }
    }
    
    [OculusField("required_space_numerical")]
    [TrackChanges]
    [BsonElement("rs")]
    public long requiredSpace { get; set; } = 0;

    [BsonIgnore]
    public string requiredSpaceFormatted
    {
        get
        {
            return SizeConverter.ByteSizeToString(requiredSpace);
        }
    }

    [OculusField("file_name")]
    [TrackChanges]
    [BsonElement("n")]
    public string filename { get; set; } = "";
    
    [OculusField("targeted_devices")]
    [TrackChanges]
    [BsonElement("t")]
    public List<string> targetedDevicesFormatted { get; set; } = new List<string>();
    
    [OculusField("targeted_devices_enum")]
    [TrackChanges]
    [BsonElement("te")]
    public List<Headset> targetedDevices { get; set; } = new List<Headset>();
    
    [OculusField("permissions")]
    [TrackChanges]
    [BsonElement("pe")]
    public List<string> permissions { get; set; } = new List<string>();
    
    [OculusField("is_pre_download_enabled")]
    [TrackChanges]
    [BsonElement("pde")]
    public bool preDownloadEnabled { get; set; } = false;
    
    [OculusField("package_name")]
    [TrackChanges]
    [BsonElement("pckn")]
    public string packageName { get; set; } = "";
    
    [BsonIgnore]
    [TrackChanges]
    public string binaryStatusFormatted {
        get
        {
            return OculusConverter.FormatOculusEnumString(binaryStatus.ToString());
        }
        
    }
    [OculusField("status_enum")]
    [TrackChanges]
    [BsonElement("bs")]
    public BinaryStatus binaryStatus { get; set; } = BinaryStatus.UNKNOWN;
    [ListScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("rc")]
    public List<DBReleaseChannel> releaseChannels { get; set; } = new List<DBReleaseChannel>();
    [BsonIgnore]
    [TrackChanges]
    public bool downloadable
    {
        get
        {
            return releaseChannels.Count > 0;
        }
    }
    [OculusField("max_android_sdk_version")]
    [TrackChanges]
    [BsonElement("maxs")]
    public int? maxAndroidSdkVersion { get; set; } = null;
    [OculusField("min_android_sdk_version")]
    [TrackChanges]
    [BsonElement("mins")]
    public int? minAndroidSdkVersion { get; set; } = null;
    [OculusField("target_android_sdk_version")]
    [TrackChanges]
    [BsonElement("ts")]
    public int? targetAndroidSdkVersion { get; set; } = null;
    [BsonElement("o")]
    public DBOBBBinary? obbBinary { get; set; } = null;
    public DBVersion? GetEntryForDiffGeneration(IEnumerable<DBVersion> collection)
    {
        return collection.FirstOrDefault(x => x.id == this.id);
    }

    public void AddOrUpdateEntry(IMongoCollection<DBVersion> collection)
    {
        collection.ReplaceOne(x => x.id == this.id, this, new ReplaceOptions() { IsUpsert = true });
    }
    public override ApplicationContext GetApplicationIds()
    {
        return ApplicationContext.FromAppId(parentApplication?.id ?? null);
    }

    public override void PopulateSelf(PopulationContext context)
    {
        alias = context.GetVersionAlias(id)?.alias ?? null;
    }
    
    public static List<DBVersion> GetVersionsOfAppId(string applicationId)
    {
        return OculusDBDatabase.versionCollection.Find(x => x.parentApplication.id == applicationId).SortByDescending(x => x.versionCode).ToList();
    }

    public static List<DBVersion> GetVersionsOfAppIds(List<string> applicationIds)
    {
        List<DBVersion> versions = new List<DBVersion>();
        foreach (string appId in applicationIds)
        {
            versions.AddRange(GetVersionsOfAppId(appId));
        }
        return versions;
    }

    public static DBVersion? ById(string id)
    {
        return OculusDBDatabase.versionCollection.Find(x => x.id == id).FirstOrDefault();
    }

    public override string GetId()
    {
        return id;
    }
}