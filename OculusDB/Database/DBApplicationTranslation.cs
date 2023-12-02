using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBApplicationTranslation : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ApplicationTranslation;
    public DBParentApplication parentApplication { get; set; } = null;
    [OculusField("id")]
    public string id { get; set; } = "";
    [OculusField("locale")]
    public string locale { get; set; } = "";
    [OculusField("display_name")]
    public string displayName { get; set; } = "";
    [OculusField("short_description")]
    public string shortDescription { get; set; } = "";
    [OculusField("long_description")]
    public string longDescription { get; set; } = "";
    [OculusField("long_description_uses_markdown")]
    public bool longDescriptionUsesMarkdown { get; set; } = false;
    [OculusField("keywords")]
    public List<string> keywords { get; set; } = new List<string>();
}