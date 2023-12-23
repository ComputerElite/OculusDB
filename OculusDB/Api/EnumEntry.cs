namespace OculusDB.ApiDocs;

public class EnumEntry
{
    public string displayName { get; set; } = "";
    public string enumName { get; set; } = "";
    public int? value { get; set; } = null;
    
    public EnumEntry() {}
    public EnumEntry(string displayName, string enumName, int value)
    {
        this.displayName = displayName;
        this.enumName = enumName;
        this.value = value;
    }
    public EnumEntry(string displayName, string enumName)
    {
        this.displayName = displayName;
        this.enumName = enumName;
    }
}