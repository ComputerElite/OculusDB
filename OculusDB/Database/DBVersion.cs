using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBVersion : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Version;
    public DBParentApplication parentApplication { get; set; } = null;
    public HeadsetBinaryType binaryType { get; set; } = HeadsetBinaryType.Unknown;
    [OculusField("id")]
    public string id { get; set; } = "";
    [OculusField("version")]
    public string version { get; set; } = "";
    
    [BsonIgnore]
    public string? alias { get; set; } = "";
    
    [OculusField("versionCode")]
    public long versionCode { get; set; } = 0;
    
    [OculusField("changeLog")]
    public string changelog { get; set; } = "";
    
    [OculusField("created_date_datetime")]
    public DateTime uploadedDate { get; set; } = DateTime.MinValue;
    
    [OculusField("size_numerical")]
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
    public string filename { get; set; } = "";
    
    [OculusField("targeted_devices")]
    public List<string> targetedDevices { get; set; } = new List<string>();
    
    [OculusField("targeted_devices_enum")]
    public List<Headset> targetedDevicesEnum { get; set; } = new List<Headset>();
    
    [OculusField("permissions")]
    public List<string> permissions { get; set; } = new List<string>();
    
    [OculusField("is_pre_download_enabled")]
    public bool preDownloadEnabled { get; set; } = false;
    
    [OculusField("package_name")]
    public string packageName { get; set; } = "";
    
    [OculusField("status")]
    public string binaryStatus { get; set; } = "";
    [OculusField("status_enum")]
    public BinaryStatus binaryStatusEnum { get; set; } = BinaryStatus.UNKNOWN;
    
    public List<DBReleaseChannel> releaseChannels { get; set; } = new List<DBReleaseChannel>();
    public DateTime lastPriorityScrape { get; set; } = DateTime.MinValue;
}