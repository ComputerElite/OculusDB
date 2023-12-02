namespace OculusDB.Database;

public class DBIAPItem : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItem;
    public DBParentApplication? parentApplication { get; set; } = null;
    [OculusField("id")]
    public string id { get; set; } = "";
}