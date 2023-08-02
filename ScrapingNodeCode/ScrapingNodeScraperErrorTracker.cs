namespace OculusDB.ScrapingNodeCode;

public class ScrapingNodeScraperErrorTracker
{
    public List<DateTime> errorTimes { get; set; } = new List<DateTime>();
    public DateTime continueTime { get; set; } = DateTime.MinValue;
    
    public bool ContinueScraping()
    {
        if(continueTime > DateTime.UtcNow) return false;
        return true;
    }

    public void AddError()
    {
        errorTimes.Add(DateTime.UtcNow);
        errorTimes.RemoveAll(x => x < DateTime.UtcNow.AddMinutes(-5));
        // If more than 10 errors occur in 5 minutes, the node will stop scraping for 5 hours
        if(errorTimes.Count > 10) continueTime = DateTime.UtcNow.AddHours(5);
    }
}