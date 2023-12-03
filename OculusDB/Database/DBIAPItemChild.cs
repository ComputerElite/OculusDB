namespace OculusDB.Database;

public class DBIAPItemChild : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IAPItemChild;
    public string id { get; set; } = "";
}