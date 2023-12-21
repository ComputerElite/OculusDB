using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBOBBBinary : DBBase
{
    [BsonElement("_dbt")]
    public override string __OculusDBType { get; set; } = DBDataTypes.ObbBinary;
    
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    
    [OculusField("file_name")]
    [TrackChanges]
    [BsonElement("fn")]
    public string filename { get; set; } = "";
    
    [OculusField("sizeNumerical")]
    [TrackChanges]
    [BsonElement("s")]
    public long size { get; set; } = 0;
    [OculusField("is_segmented")]
    [TrackChanges]
    [BsonElement("iss")]
    public bool isSegmented { get; set; } = false;

    [BsonIgnore]
    public string sizeFormatted
    {
        get
        {
            return SizeConverter.ByteSizeToString(size);
        }
    }

    public override string GetId()
    {
        return id;
    }
}