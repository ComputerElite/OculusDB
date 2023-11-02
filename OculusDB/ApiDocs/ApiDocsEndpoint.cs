namespace OculusDB.ApiDocs;

public class ApiDocsEndpoint
{
    public string url { get; set; } = "";
    public string method { get; set; } = "";
    public string description { get; set; } = "";
    public List<ApiDocsParameter> parameters { get; set; } = new List<ApiDocsParameter>();
    public string exampleUrl { get; set; } = "";
    public dynamic exampleResponse { get; set; } = null;
        
}