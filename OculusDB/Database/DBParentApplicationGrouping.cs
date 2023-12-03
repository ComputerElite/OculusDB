namespace OculusDB.Database;

public class DBParentApplicationGrouping : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ParentApplicationGrouping;
    [OculusField("id")]
    public string id { get; set; } = "";
}