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
public class SearchResultWithType<T>
{
    public List<T> results { get; set; } = new List<T>();
    public string? resultAnnotation { get; set; } = null;
    
    public SearchResultWithType() {}

    public SearchResultWithType(string msg)
    {
        resultAnnotation = msg;
    }
    public SearchResultWithType(List<T> res, string msg)
    {
        results = res;
        resultAnnotation = msg;
    }
}