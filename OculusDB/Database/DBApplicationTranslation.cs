using MongoDB.Bson.Serialization.Attributes;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Database;

public class DBApplicationTranslation : DBBase
{
    public override string __OculusDBType { get; set; } = DBDataTypes.ApplicationTranslation;
    [ObjectScrapingNodeFieldPresent]
    [TrackChanges]
    [BsonElement("p")]
    public DBParentApplication? parentApplication { get; set; } = null;
    [OculusField("id")]
    [TrackChanges]
    [BsonElement("id")]
    public string id { get; set; } = "";
    [OculusField("locale")]
    [TrackChanges]
    [BsonElement("l")]
    public string locale { get; set; } = "";
    [OculusField("display_name")]
    [TrackChanges]
    [BsonElement("n")]
    public string displayName { get; set; } = "";
    [OculusField("short_description")]
    [TrackChanges]
    [BsonElement("d")]
    public string shortDescription { get; set; } = "";
    [OculusField("long_description")]
    [TrackChanges]
    [BsonElement("ld")]
    public string longDescription { get; set; } = "";
    [OculusField("long_description_uses_markdown")]
    [TrackChanges]
    [BsonElement("m")]
    public bool longDescriptionUsesMarkdown { get; set; } = false;
    [OculusField("keywords")]
    [TrackChanges]
    [BsonElement("k")]
    public List<string> keywords { get; set; } = new List<string>();
}