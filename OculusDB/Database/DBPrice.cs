using OculusDB.ObjectConverters;

namespace OculusDB.Database;

public class DBPrice : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Price;
    [TrackChanges]
    public string currency { get; set; } = "";
    [TrackChanges]
    public string priceFormatted { get; set; } = "";
    [TrackChanges]
    public long price { get; set; } = 0;
}