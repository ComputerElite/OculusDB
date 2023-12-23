using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBAssetFile : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AssetFile;
    
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("p")]
    public DBParentApplication? parentApplication { get; set; } = null;
    
    [OculusField("file_name")]
    [TrackChanges]
    [BsonElement("n")]
    public string fileName { get; set; } = "";
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [TrackChanges]
    [OculusField("created_date_datetime")]
    [BsonElement("d")]
    public DateTime uploadDate { get; set; } = DateTime.MinValue;
    [TrackChanges]
    [BsonElement("hg")]
    public HeadsetGroup? group { get; set; } = null;

    public override string GetId()
    {
        return id;
    }
}