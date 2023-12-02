namespace OculusDB.Database;

public class DBBase
{
    public DateTime __lastUpdated { get; set; } = DateTime.Now;
    public virtual string __OculusDBType { get; set; } = DBDataTypes.Unknown;
    public string __sn { get; set; } = "";
}