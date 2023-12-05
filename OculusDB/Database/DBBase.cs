using OculusDB.MongoDB;

namespace OculusDB.Database;

// DB Base should never be tracked as it just contains metadata of Scraping

public class DBBase
{
    public DateTime __lastUpdated { get; set; } = DateTime.Now;
    public virtual string __OculusDBType { get; set; } = DBDataTypes.Unknown;
    public string __sn { get; set; } = "";

    public virtual List<string> GetApplicationIds()
    {
        return new List<string>();
    }
    
    public virtual void PopulateSelf(PopulationContext context)
    {
        return;
    }
}