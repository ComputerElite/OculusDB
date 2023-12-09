using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBError : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Error;
    public DBErrorType type { get; set; } = DBErrorType.CouldNotScrapeIaps;
    [BsonIgnore]
    public string typeFormatted
    {
        get
        {
            return OculusConverter.FormatDBEnumString(type.ToString());
        }
    }
    public DBErrorReason reason { get; set; } = DBErrorReason.Unknown;
    [BsonIgnore]
    public string reasonFormatted
    {
        get
        {
            return OculusConverter.FormatDBEnumString(reason.ToString());
        }
    }
    public string message { get; set; } = "";
}

public enum DBErrorType
{
    Unknown = -1,
    CouldNotScrapeIaps = 0,
    CouldNotScrapeAchievements = 1,
    StoreDlcsNotFoundInExistingDlcs = 2,
}

public enum DBErrorReason
{
    Unknown = -1,
    GroupingNull = 0,
    DlcNotInDlcList = 0,
}