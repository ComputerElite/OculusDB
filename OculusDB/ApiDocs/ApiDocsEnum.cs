namespace OculusDB.ApiDocs;

public class ApiDocsEnum
{
    public string name { get; set; } = "";
    public string description { get; set; } = "";
    public List<string> usedOn { get; set; } = new List<string>();
    public List<ApiDocsEnumValue> values { get; set; } = new List<ApiDocsEnumValue>();
}