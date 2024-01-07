using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBParentApplicationGrouping : DBBase
{

    public override string __OculusDBType { get; set; } = DBDataTypes.ParentApplicationGrouping;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [BsonIgnore]
    private List<DBParentApplication> _applications = new List<DBParentApplication>();
    [BsonElement("a")]
    public List<DBParentApplication> applications {
        get
        {
            return _applications;
        }
        set
        {
            applicationIds = applications.Select(x => x.id).ToList();
            _applications = value;
        }
    }

    [BsonElement("aid")]
    public List<string> applicationIds { get; set; } = new List<string>();
}