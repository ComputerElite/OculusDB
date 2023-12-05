using OculusDB.Database;

namespace OculusDB.MongoDB;

public class ApplicationContext
{
    public string groupingId { get; set; } = "";
    public List<string> appIds { get; set; } = new List<string>();
    
    public static ApplicationContext FromAppId(string? appId)
    {
        if (appId == null) return new ApplicationContext();
        return new ApplicationContext()
        {
            groupingId = "",
            appIds = new List<string>() { appId }
        };
    }
    
    public static ApplicationContext FromGroupingId(string? groupingId)
    {
        if (groupingId == null) return new ApplicationContext();
        return new ApplicationContext()
        {
            groupingId = groupingId,
            appIds = DBApplicationGrouping.GetApplicationIdsFromGrouping(groupingId)
        };
    }
}