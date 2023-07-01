using MongoDB.Bson.Serialization.Attributes;
using OculusGraphQLApiLib;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBAppImage
{
    public DateTime __lastUpdated { get; set; } = DateTime.Now;
    public string __OculusDBType { get; set; } = DBDataTypes.AppImage;

    /// <summary>
    /// Scraping node ID
    /// </summary>
    public string __sn { get; set; } = "";
    public string appId { get; set; } = "";
    public string mimeType { get; set; } = "image/webp";
    public byte[] data { get; set; } = new byte[0];
}