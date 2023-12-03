using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBParentApplication : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ParentApplication;
    [TrackChanges]
    public string id { get; set; } = "";
    [TrackChanges]
    public string displayName { get; set; } = "";
}