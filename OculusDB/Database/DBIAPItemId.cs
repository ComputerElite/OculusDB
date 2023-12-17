using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBIAPItemId : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.IapItemId;
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
}