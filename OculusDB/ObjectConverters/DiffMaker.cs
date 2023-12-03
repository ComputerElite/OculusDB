using System.Reflection;
using System.Text.Json.Serialization;
using ComputerUtils.Logging;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.Database;

namespace OculusDB.ObjectConverters;

public class DiffMaker
{
    public static DBDifference GetDifference(object? oldObject, object? newObject, int depth = 0)
    {
        DBDifference diff = new DBDifference();
        diff.depth = depth;
        if (depth == 0)
        {
            // Add old and new object to root diff
            diff.oldObject = oldObject;
            diff.newObject = newObject;
        }

        // various null checks
        if (oldObject == null && newObject == null) return diff;
        if(oldObject == null && newObject != null) return diff.AddEntry(new DBDifferenceEntry("", oldObject, newObject));
        if(oldObject != null && newObject == null) return diff.AddEntry(new DBDifferenceEntry("", oldObject, newObject));
        
        Type comparisonType = oldObject.GetType();
        // Check default C# types and return if they're different
        if(comparisonType != newObject?.GetType()) return diff.AddEntry(new DBDifferenceEntry("", oldObject, newObject));
        if(comparisonType.IsPrimitive) return diff.ConditionalAddEntry(oldObject.Equals(newObject), new DBDifferenceEntry("", oldObject, newObject));
        if(comparisonType.IsEnum) return diff.ConditionalAddEntry(oldObject.Equals(newObject), new DBDifferenceEntry("", oldObject, newObject));
        if(comparisonType == typeof(string)) return diff.ConditionalAddEntry(oldObject.Equals(newObject), new DBDifferenceEntry("", oldObject, newObject));
        if(comparisonType == typeof(DateTime)) return diff.ConditionalAddEntry(oldObject.Equals(newObject), new DBDifferenceEntry("", oldObject, newObject));
        // Compare lists
        if (comparisonType.IsGenericType && (comparisonType.GetGenericTypeDefinition() == typeof(List<>)))
        {
            // Implements list
            return diff.Merge(GetListDiff(oldObject, newObject, comparisonType, depth + 1), ".");
        }
        
        // Fallback to object, check all tracked properties
        IEnumerable<PropertyInfo> properties = oldObject.GetType().GetProperties()
            .Where(prop => prop.IsDefined(typeof(TrackChanges), false));
        foreach (PropertyInfo property in properties)
        {
            object? oldValue = property.GetValue(oldObject);
            object? newValue = property.GetValue(newObject);
            DBDifference propertyDiff = GetDifference(oldValue, newValue, depth + 1);
            diff.Merge(propertyDiff, (depth > 0 ? "." : "") + property.Name);
        }

        return diff;
    }

    public static DBDifference GetListDiff(object oldList, object newList, Type listType, int depth)
    {
        DBDifference diff = new DBDifference();
        diff.depth = depth;
        // Use reflection to get the Count property and iterate through the list
        PropertyInfo countProperty = listType.GetProperty("Count");
        int oldCount = (int)countProperty.GetValue(oldList);
        int newCount = (int)countProperty.GetValue(newList);
        if(oldCount != newCount) return diff.AddEntry(new DBDifferenceEntry("Count", oldCount, newCount));
        // Iterate through the list elements
        for (int i = 0; i < oldCount; i++)
        {
            PropertyInfo itemPropertyInfo = listType.GetProperty("Item");
            object oldListItem = itemPropertyInfo.GetValue(oldList, new object[] { i });
            object newListItem = itemPropertyInfo.GetValue(newList, new object[] { i });
            // Get difference of list elements and merge
            DBDifference itemDiff = GetDifference(oldListItem, newListItem, depth + 1);
            diff.Merge(itemDiff, "[" + i + "]");
        }
        return diff;
    }
}

public class TrackChanges : Attribute
{
    
}

public enum DifferenceType
{
    ObjectAdded,
    ObjectUpdated,
    ObjectRemoved,
    FuckedUp
}

public class DBDifference
{
    [BsonIgnore]
    public bool isSame
    {
        get
        {
            return entries.Count == 0;
        }
    }
    public object? oldObject { get; set; } = null;
    public object? newObject { get; set; } = null;
    public DifferenceType differenceType
    {
        get
        {
            if (oldObject == null && newObject == null) return DifferenceType.FuckedUp;
            if (oldObject == null && newObject != null) return DifferenceType.ObjectRemoved; // realistically this should never happen
            if (oldObject != null && newObject == null) return DifferenceType.ObjectAdded;
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
}

public class DBDifferenceEntry
{
    public string name { get; set; } = "";
    public object? oldValue { get; set; } = null;
    public object? newValue { get; set; } = null;
    
    public DBDifferenceEntry(string name, object? oldValue, object? newValue)
    {
        this.name = name;
        this.oldValue = oldValue;
        this.newValue = newValue;
    }
}