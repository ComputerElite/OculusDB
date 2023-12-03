using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBOBBBinary : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.OBBBinary;
    
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    
    [OculusField("file_name")]
    [TrackChanges]
    public string filename { get; set; } = "";
    
    [OculusField("sizeNumerical")]
    [TrackChanges]
    public long size { get; set; } = 0;
    [OculusField("is_segmented")]
    [TrackChanges]
    public bool isSegmented { get; set; } = false;

    [BsonIgnore]
    public string sizeFormatted
    {
        get
        {
            return SizeConverter.ByteSizeToString(size);
        }
    }
}