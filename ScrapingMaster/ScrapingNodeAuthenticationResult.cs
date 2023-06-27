namespace OculusDB.ScrapingMaster;

public class ScrapingNodeAuthenticationResult
{
    public string msg { get; set; } = "";
    public bool tokenAuthorized { get; set; } = false;
    public bool tokenExpired { get; set; } = true;
    public bool tokenValid { get; set; } = false;
    public ScrapingNode scrapingNode { get; set; } = new();
}