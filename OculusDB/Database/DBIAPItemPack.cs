namespace OculusDB.Database;

public class DBIAPItemPack : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItemPack;
    [ObjectScrapingNodeFieldPresent]
    public DBParentApplication? parentApplication { get; set; } = null;
    [OculusField("id")]
    public string id { get; set; } = "";
}