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
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBParentApplication? parentApplication { get; set; } = null;
    [TrackChanges]
    public HeadsetBinaryType binaryType { get; set; } = HeadsetBinaryType.Unknown;
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    [OculusField("version")]
    [TrackChanges]
    public string version { get; set; } = "";
    
    [BsonIgnore]
    public string? alias { get; set; } = "";
    
    [OculusField("versionCode")]
    [TrackChanges]
    public long versionCode { get; set; } = 0;
    
    [OculusField("changeLog")]
    [TrackChanges]
    public string changelog { get; set; } = "";
    
    [OculusField("created_date_datetime")]
    [TrackChanges]
    public DateTime uploadedDate { get; set; } = DateTime.MinValue;
    
    [OculusField("size_numerical")]
    [TrackChanges]
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
    public string filename { get; set; } = "";
    
    [OculusField("targeted_devices")]
    [TrackChanges]
    public List<string> targetedDevices { get; set; } = new List<string>();
    
    [OculusField("targeted_devices_enum")]
    [TrackChanges]
    public List<Headset> targetedDevicesEnum { get; set; } = new List<Headset>();
    
    [OculusField("permissions")]
    [TrackChanges]
    public List<string> permissions { get; set; } = new List<string>();
    
    [OculusField("is_pre_download_enabled")]
    [TrackChanges]
    public bool preDownloadEnabled { get; set; } = false;
    
    [OculusField("package_name")]
    [TrackChanges]
    public string packageName { get; set; } = "";
    
    [OculusField("status")]
    [TrackChanges]
    public string binaryStatus { get; set; } = "";
    [OculusField("status_enum")]
    [TrackChanges]
    public BinaryStatus binaryStatusEnum { get; set; } = BinaryStatus.UNKNOWN;
    [ListScrapingNodeFieldPresent]
    [TrackChanges]
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
    public int? maxAndroidSdkVersion { get; set; } = null;
    [OculusField("min_android_sdk_version")]
    [TrackChanges]
    public int? minAndroidSdkVersion { get; set; } = null;
    [OculusField("target_android_sdk_version")]
    [TrackChanges]
    public int? targetAndroidSdkVersion { get; set; } = null;
    public DBOBBBinary? obbBinary { get; set; } = null;
    public DateTime lastPriorityScrape { get; set; } = DateTime.MinValue;
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
}