using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBApplicationGrouping : DBBase
{
    [BsonElement("_dbt")]
    public override string __OculusDBType { get; set; } = DBDataTypes.ApplicationGrouping;
    [OculusField("id")]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [OculusField("report_method_enum")]
    [BsonElement("rm")]
    public ReportMethod reportMethod { get; set; } = new ReportMethod();

    [BsonIgnore]
    public string reportMethodFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(reportMethod.ToString());
        }
    }

    public static List<string> GetApplicationIdsFromGrouping(string? groupingId)
    { 
        if(groupingId == null) return new List<string>();
        return OculusDBDatabase.applicationCollection.Find(x => x.grouping != null && x.grouping.id == groupingId).ToList()
            .ConvertAll(x => x.id);
    }
}