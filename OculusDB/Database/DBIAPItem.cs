namespace OculusDB.Database;

public class DBIAPItem : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItem;
    [ObjectScrapingNodeFieldPresent]
    public DBParentApplication? parentApplication { get; set; } = null;
    [OculusField("id")]
    public string id { get; set; } = "";
}