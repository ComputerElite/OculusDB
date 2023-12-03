namespace OculusDB.Database;

public class DBAssetFile : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AssetFile;
    
    [ObjectScrapingNodeFieldPresent]
    public DBParentApplication? parentApplication { get; set; } = null;
    
    [OculusField("file_name")]
    public string fileName { get; set; } = "";
    [OculusField("id")]
    public string id { get; set; } = "";
    [OculusField("created_date_datetime")]
    public DateTime uploadDate { get; set; } = DateTime.MinValue;
    public HeadsetGroup? group { get; set; } = null;
}