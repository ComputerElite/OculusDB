using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBPrice : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Price;
    [TrackChanges]
    [BsonElement("c")]
    public string currency { get; set; } = "";
    [TrackChanges]
    [BsonElement("f")]
    public string priceFormatted { get; set; } = "";
    [TrackChanges]
    [BsonElement("p")]
    public long price { get; set; } = 0;
}