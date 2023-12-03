using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBReleaseChannel : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ReleaseChannel;
    [TrackChanges]
    public string id { get; set; } = "";
    [TrackChanges]
    public string name { get; set; } = "";
}