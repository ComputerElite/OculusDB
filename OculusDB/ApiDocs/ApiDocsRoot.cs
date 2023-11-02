namespace OculusDB.ApiDocs;

public class ApiDocsRoot
{
    public List<ApiDocsEnum> enums { get; set; } = new List<ApiDocsEnum>();
    public List<ApiDocsEndpoint> endpoints { get; set; } = new List<ApiDocsEndpoint>();
}