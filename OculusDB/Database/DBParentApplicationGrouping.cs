using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBParentApplicationGrouping : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ParentApplicationGrouping;
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    public List<DBParentApplication> applications { get; set; } = new List<DBParentApplication>();
}