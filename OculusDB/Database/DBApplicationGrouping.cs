using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBApplicationGrouping : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ApplicationGrouping;
    [OculusField("id")]
    public string id { get; set; } = "";
    [OculusField("report_method_enum")]
    public ReportMethod reportMethod { get; set; } = new ReportMethod();

    [BsonIgnore]
    public string reportMethodFormatted
    {
        get
        {
            return OculusConverter.FormatOculusEnumString(reportMethod.ToString());
        }
    }
}