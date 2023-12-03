using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBApplicationTranslation : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ApplicationTranslation;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    public DBParentApplication? parentApplication { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    public string id { get; set; } = "";
    [OculusField("locale")]
    [TrackChanges]
    public string locale { get; set; } = "";
    [OculusField("display_name")]
    [TrackChanges]
    public string displayName { get; set; } = "";
    [OculusField("short_description")]
    [TrackChanges]
    public string shortDescription { get; set; } = "";
    [OculusField("long_description")]
    [TrackChanges]
    public string longDescription { get; set; } = "";
    [OculusField("long_description_uses_markdown")]
    [TrackChanges]
    public bool longDescriptionUsesMarkdown { get; set; } = false;
    [OculusField("keywords")]
    [TrackChanges]
    public List<string> keywords { get; set; } = new List<string>();
}