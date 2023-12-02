namespace OculusDB.Database;

public class DBReleaseChannel : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ReleaseChannel;
    public string id { get; set; } = "";
    public string name { get; set; } = "";
}