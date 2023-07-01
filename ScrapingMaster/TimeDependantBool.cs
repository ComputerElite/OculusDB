namespace OculusDB.ScrapingMaster;

public class TimeDependantBool
{
    public bool value { get; set; } = false;
    public DateTime lastSet { get; set; } = DateTime.MinValue;
    public TimeSpan validFor { get; set; } = TimeSpan.Zero;
    public DateTime validUntil => lastSet + validFor;
    public string responsibleScrapingNodeId { get; set; } = "";
    
    /// <summary>
    /// Set the time dependant bool to the provided value for the provided time
    /// </summary>
    /// <param name="value"></param>
    /// <param name="setToFalseIn"></param>
    /// <param name="responsibleScrapingNodeId"></param>
    public void Set(bool value, TimeSpan setToFalseIn, string responsibleScrapingNodeId)
    {
        this.value = value;
        lastSet = DateTime.UtcNow;
        validFor = setToFalseIn;
        this.responsibleScrapingNodeId = responsibleScrapingNodeId;
    }

    /// <summary>
    /// If the value is true and hasn't expired yet
    /// </summary>
    /// <returns></returns>
    public bool IsTrueAndValid()
    {
        return value && DateTime.UtcNow < validUntil;
    }

    public static implicit operator bool(TimeDependantBool b)
    {
        return b.IsTrueAndValid();
    }

    /// <summary>
    /// returns true if this node is responsible for this value
    /// </summary>
    /// <param name="scrapingNode">node to check</param>
    /// <returns></returns>
    public bool IsThisResponsible(ScrapingNode scrapingNode)
    {
        return scrapingNode.scrapingNodeId == responsibleScrapingNodeId;
    }

    /// <summary>
    /// Set value to false if the provided scraping node is responsible for this value
    /// </summary>
    /// <param name="scrapingNode">Node to check</param>
    public void Unlock(ScrapingNode scrapingNode)
    {
        if (IsThisResponsible(scrapingNode)) value = false;
    }
}