using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;

namespace OculusDB.Database;

public class DBOBBBinary : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.OBBBinary;
    
    [OculusField("id")]
    public string id { get; set; } = "";
    
    [OculusField("file_name")]
    public string filename { get; set; } = "";
    
    [OculusField("sizeNumerical")]
    public long size { get; set; } = 0;

    [BsonIgnore]
    public string sizeFormatted
    {
        get
        {
            return SizeConverter.ByteSizeToString(size);
        }
    }
}