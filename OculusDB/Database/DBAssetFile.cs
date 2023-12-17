using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBAssetFile : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AssetFile;
    
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBParentApplication? parentApplication { get; set; } = null;
    
    [OculusField("file_name")]
    [TrackChanges]
    public string fileName { get; set; } = "";
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    [TrackChanges]
    [OculusField("created_date_datetime")]
    public DateTime uploadDate { get; set; } = DateTime.MinValue;
    [TrackChanges]
    public HeadsetGroup? group { get; set; } = null;

    public override string GetId()
    {
        return id;
    }
}