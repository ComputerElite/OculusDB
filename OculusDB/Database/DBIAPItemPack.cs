namespace OculusDB.Database;

public class DBIAPItemPack : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItemPack;
    [ObjectScrapingNodeFieldPresent]
    public DBParentApplicationGrouping? grouping { get; set; } = null;
    [OculusField("id")]
    public string id { get; set; } = "";
    public List<DBIAPItemChild> items { get; set; } = new List<DBIAPItemChild>();
}