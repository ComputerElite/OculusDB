namespace OculusDB.Search;

public class SearchResult
{
    public List<dynamic> results { get; set; } = new List<dynamic>();
    public string? resultAnnotation { get; set; } = null;
    
    public SearchResult() {}

    public SearchResult(string msg)
    {
        resultAnnotation = msg;
    }
    public SearchResult(List<dynamic> res, string msg)
    {
        results = res;
        resultAnnotation = msg;
    }
}