namespace OculusDB.Database;

public class DBPrice : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.Price;
    public DBParentApplication parentApplication { get; set; } = new DBParentApplication();
    public string currency { get; set; } = "";
    public string formattedPrice { get; set; } = "";
    public long price { get; set; } = 0;
    public string offerId { get; set; } = "";
}