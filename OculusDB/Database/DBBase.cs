using MongoDB.Bson.Serialization.Attributes;
using OculusDB.MongoDB;

namespace OculusDB.Database;

// DB Base should never be tracked as it just contains metadata of Scraping

public class DBBase
{
    [BsonElement("_lu")]
    public DateTime __lastUpdated { get; set; } = DateTime.Now;
    [BsonElement("_dbt")]
    public virtual string __OculusDBType { get; set; } = DBDataTypes.Unknown;
    [BsonElement("_sn")]
    public string __sn { get; set; } = "";

    public virtual ApplicationContext GetApplicationIds()
    {
        return new ApplicationContext();
    }
    
    public virtual void PopulateSelf(PopulationContext context)
    {
        return;
    }

    public virtual string GetId()
    {
        return "";
    }
}