namespace OculusDB.Database;

public class DBParentApplication : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ParentApplication;
    public string id { get; set; } = "";
    public string displayName { get; set; } = "";
}