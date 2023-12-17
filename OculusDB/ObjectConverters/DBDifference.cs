using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using OculusDB.Database;

namespace OculusDB.ObjectConverters;

public enum DifferenceType
{
    FuckedUp = -1,
    ObjectAdded = 0,
    ObjectUpdated = 1,
    ObjectRemoved = 2,
}

public class DBDifference : DBBase, IDBObjectOperations<DBDifference>
{
    [BsonId]
    public ObjectId _id { get; set; }
    public override string __OculusDBType { get; set; } = DBDataTypes.Difference;
    public string entryId { get; set; } = "";
    public string entryOculusDBType { get; set; } = "";
    

    [BsonIgnore]
    public bool isSame
    {
        get
        {
            return entries.Count == 0;
        }
    }
    [JsonIgnore]
    public object? oldObject { get; set; } = null;
    [JsonIgnore]
    public object? newObject { get; set; } = null;

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
    public string name { get; set; } = "";
    public object? oldValue { get; set; } = null;
    public object? newValue { get; set; } = null;
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