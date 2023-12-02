using MongoDB.Bson.Serialization.Attributes;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBAppImage : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.AppImage;
    public DBParentApplication parentApplication { get; set; } = new DBParentApplication();
    public string mimeType { get; set; } = "image/webp";
    public byte[] data { get; set; } = new byte[0];
}