using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using OculusDB.Database;
using OculusDB.MongoDB;

namespace OculusDB.ObjectConverters;

public enum DifferenceType
{
    FuckedUp = -1,
    ObjectAdded = 0,
    ObjectUpdated = 1,
    ObjectRemoved = 2,
}

public enum DifferenceNameType
{
    Unknown = -1,
    ApplicationAdded = 0,
    ApplicationUpdated = 1,
    ApplicationOfferChanged = 2,
    ApplicationOfferChangedAndUpdated = 3,
    
    AchievementAdded = 4,
    AchievementUpdated = 5,
    
    OfferAdded = 6,
    OfferUpdated = 7,
    OfferSaleStarted = 20,
    OfferSaleEnded = 21,
    
    VersionAdded = 8,
    VersionUpdated = 9,
    VersionDownloadable = 10,
    VersionUnpublished = 11, // this should never happen but may as well track it
    
    IapItemAdded = 12,
    IapItemUpdated = 13,
    IapItemOfferChanged = 14,
    IapItemOfferChangedAndUpdated = 15,
    
    IapItemPackAdded = 16,
    IapItemPackUpdated = 17,
    IapItemPackOfferChanged = 18,
    IapItemPackOfferChangedAndUpdated = 19
}

public class DBDifference : DBBase, IDBObjectOperations<DBDifference>
{
    [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
    [BsonRepresentation(BsonType.ObjectId)]
    public string __id { get; set; }
    public override string __OculusDBType { get; set; } = DBDataTypes.Difference;
    [BsonElement("ei")]
    public string entryId { get; set; } = "";
    [BsonElement("et")]
    public string entryOculusDBType { get; set; } = "";
    [BsonElement("ea")]
    public List<string> entryParentApplicationIds { get; set; } = new List<string>();
    [BsonElement("dn")]
    public DifferenceNameType differenceName { get; set; } = DifferenceNameType.Unknown;
    [BsonElement("wp")]
    public bool webhookProcessed { get; set; } = false;
    [BsonIgnore]
    public string differenceNameFormatted
    {
        get
        {
            return OculusConverter.FormatDBEnumString(differenceName.ToString());
        }
    }

    public void PopulateDifferenceName()
    {
        switch (entryOculusDBType)
        {
            case DBDataTypes.IapItemPack:
                if(differenceType == DifferenceType.ObjectAdded)
                {
                    differenceName = DifferenceNameType.IapItemPackAdded;
                    return;
                }
                // Check if offer changed
                bool isOfferChangeIapPack = entries.Any(x => x.name == "offerId");
                bool isUpdateIapPack = entries.Count(x => x.name != "offerId") > 0;
                if(isOfferChangeIapPack && isUpdateIapPack)differenceName = DifferenceNameType.IapItemPackOfferChangedAndUpdated;
                else if(isOfferChangeIapPack) differenceName = DifferenceNameType.IapItemPackOfferChanged;
                else if(isUpdateIapPack) differenceName = DifferenceNameType.IapItemPackUpdated;
                else differenceName = DifferenceNameType.Unknown;
                break;
            case DBDataTypes.IapItem:
                // IapItem got released when it's added
                if (differenceType == DifferenceType.ObjectAdded)
                {
                    differenceName = DifferenceNameType.IapItemAdded;
                    return;
                }
                // Check if offer changed
                bool isOfferChangeIap = entries.Any(x => x.name == "offerId");
                bool isUpdateIap = entries.Count(x => x.name != "offerId") > 0;
                if(isOfferChangeIap && isUpdateIap)differenceName = DifferenceNameType.IapItemOfferChangedAndUpdated;
                else if(isOfferChangeIap) differenceName = DifferenceNameType.IapItemOfferChanged;
                else if(isUpdateIap) differenceName = DifferenceNameType.IapItemUpdated;
                else differenceName = DifferenceNameType.Unknown;
                break;
            case DBDataTypes.Version:
                bool downloadable = entries.Any(x => x.name == "downloadable" && (bool)x.newValue == true);
                bool unpublished = entries.Any(x => x.name == "downloadable" && (bool)x.newValue == false);
                if (differenceType == DifferenceType.ObjectAdded) differenceName = DifferenceNameType.VersionAdded;
                else if (downloadable) differenceName = DifferenceNameType.VersionDownloadable;
                else if (unpublished) differenceName = DifferenceNameType.VersionUnpublished;
                else differenceName = DifferenceNameType.VersionUpdated;
                break;
            case DBDataTypes.Offer:
                differenceName = differenceType == DifferenceType.ObjectAdded ? DifferenceNameType.OfferAdded : DifferenceNameType.OfferUpdated;
                // Sales happen when a strikethrough price is added.
                bool saleStarted = entries.Any(x =>
                {
                    return x.name == "strikethroughPrice" && x.oldValue == null && x.newValue != null;
                });
                bool saleEnded = entries.Any(x =>
                {
                    return x.name == "strikethroughPrice" && x.newValue == null && x.oldValue != null;
                });
                if(saleStarted) differenceName = DifferenceNameType.OfferSaleStarted;
                else if(saleEnded) differenceName = DifferenceNameType.OfferSaleEnded;
                break;
            case DBDataTypes.Achievement:
                differenceName = differenceType == DifferenceType.ObjectAdded ? DifferenceNameType.AchievementAdded : DifferenceNameType.AchievementUpdated;
                break;
            case DBDataTypes.Application:
                // Application got released when it's added
                if (differenceType == DifferenceType.ObjectAdded)
                {
                    differenceName = DifferenceNameType.ApplicationAdded;
                    return;
                }
                // Check if offer changed
                bool isOfferChange = entries.Any(x => x.name == "offerId");
                bool isUpdate = entries.Count(x => x.name != "offerId") > 0;
                if(isOfferChange && isUpdate)differenceName = DifferenceNameType.ApplicationOfferChangedAndUpdated;
                else if(isOfferChange) differenceName = DifferenceNameType.ApplicationOfferChanged;
                else if(isUpdate) differenceName = DifferenceNameType.ApplicationUpdated;
                else differenceName = DifferenceNameType.Unknown;
                break;
        }
    }
    

    [BsonIgnore]
    public bool isSame
    {
        get
        {
            return entries.Count == 0;
        }
    }
    [JsonIgnore]
    [BsonElement("o")]
    public object? oldObject { get; set; } = null;
    [JsonIgnore]
    [BsonElement("n")]
    public object? newObject { get; set; } = null;

    [BsonElement("dt")]
    public DifferenceType differenceType
    {
        get
        {
            
            if (oldObject == null && newObject == null) return DifferenceType.FuckedUp;
            else if (oldObject != null && newObject == null) return DifferenceType.ObjectRemoved; // realistically this should never happen
            else if (oldObject == null && newObject != null) return DifferenceType.ObjectAdded;
            return DifferenceType.ObjectUpdated;
        }
    }

    public string differenceTypeFormatted
    {
        get
        {
            return OculusConverter.FormatDBEnumString(differenceType.ToString());
        }
    }

    /// <summary>
    /// Used to keep track of depth of the difference
    /// </summary>
    [JsonIgnore]
    [BsonIgnore]
    public int depth { get; set; } = 0;
    [BsonElement("e")]

    public List<DBDifferenceEntry> entries { get; set; } = new List<DBDifferenceEntry>();

    public DBDifference AddEntry(DBDifferenceEntry e)
    {
        entries.Add(e);
        return this;
    }
    
    
    public DBDifference ConditionalAddEntry(bool isSame, DBDifferenceEntry e)
    {
        if(!isSame) entries.Add(e);
        return this;
    }
    
    public DBDifference Merge(DBDifference difference, string nameSectionToAdd)
    {
        for(int i = 0; i < difference.entries.Count; i++)
        {
            difference.entries[i].name = nameSectionToAdd + difference.entries[i].name;
        }
        entries.AddRange(difference.entries);
        return this;
    }

    public DBDifference? GetEntryForDiffGeneration(IEnumerable<DBDifference> collection)
    {
        return this;
    }

    public void AddOrUpdateEntry(IMongoCollection<DBDifference> collection)
    {
        collection.InsertOne(this);
    }

    public Dictionary<string, string?> GetDiscordEmbedFields()
    {
        return new Dictionary<string, string?>();
    }

    public static DBDifference? ById(string objectId)
    {
        return OculusDBDatabase.differenceCollection.Find(x => x.__id == objectId).FirstOrDefault();
    }
}

public enum DifferenceReason
{
    Unknown = -1,
    TypeChanged = 0,
    ValueChanged = 1,
    ListLengthChanged = 2,
    ObjectAdded = 3,
    ObjectRemoved = 4,
}

public class DBDifferenceEntry
{
    [BsonElement("f")]
    public string name { get; set; } = "";
    [BsonElement("o")]
    public object? oldValue { get; set; } = null;
    [BsonElement("n")]
    public object? newValue { get; set; } = null;
    [BsonElement("r")]
    public DifferenceReason reason { get; set; } = DifferenceReason.Unknown;
    [BsonIgnore]
    public string reasonFormatted
    {
        get
        {
            return OculusConverter.FormatDBEnumString(reason.ToString());
        }
    }
    
    public DBDifferenceEntry(string name, object? oldValue, object? newValue, DifferenceReason reason)
    {
        this.name = name;
        this.oldValue = oldValue;
        this.newValue = newValue;
        this.reason = reason;
    }
}

public class TrackChanges : Attribute
{
    
}