namespace OculusDB.ScrapingMaster;

public class TimeDependantBool
{
    public bool value { get; set; } = false;
    public DateTime lastSet { get; set; } = DateTime.MinValue;
    public TimeSpan validFor { get; set; } = TimeSpan.Zero;
    public DateTime validUntil => lastSet + validFor;
    public string responsibleScrapingNodeId { get; set; } = "";
    
    public void Set(bool value, TimeSpan setToFalseIn, string responsibleScrapingNodeId)
    {
        this.value = value;
        lastSet = DateTime.Now;
        validFor = setToFalseIn;
        this.responsibleScrapingNodeId = responsibleScrapingNodeId;
    }

    public bool IsTrueAndValid()
    {
        return value && DateTime.Now < validUntil;
    }

    public static implicit operator bool(TimeDependantBool b)
    {
        return b.IsTrueAndValid();
    }
}