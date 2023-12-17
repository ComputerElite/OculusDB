using System.Reflection;
using System.Text.Json.Serialization;
using ComputerUtils.Logging;
using MongoDB.Bson.Serialization.Attributes;
using OculusDB.Database;

namespace OculusDB.ObjectConverters;

public class DiffMaker
{
    public static DBDifference GetDifference(object? oldObject, object? newObject, string scrapingNodeId)
    {
        DBDifference diff = OculusConverter.AddScrapingNodeName(GetDifference(oldObject, newObject, 0), scrapingNodeId) ?? new DBDifference();
        if (newObject != null && newObject.GetType().IsAssignableTo(typeof(DBBase)))
        {
            DBBase dbBase = (DBBase)newObject;
            diff.entryId = dbBase.GetId();
            diff.entryOculusDBType = dbBase.__OculusDBType;
        }

        return diff;
    }
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
        if(oldObject == null && newObject != null) return diff.AddEntry(new DBDifferenceEntry("", oldObject, newObject, DifferenceReason.ObjectAdded));
        if(oldObject != null && newObject == null) return diff.AddEntry(new DBDifferenceEntry("", oldObject, newObject, DifferenceReason.ObjectRemoved));
        
        Type comparisonType = oldObject.GetType();
        // Check default C# types and return if they're different
        if(comparisonType != newObject?.GetType()) return diff.AddEntry(new DBDifferenceEntry("", oldObject, newObject, DifferenceReason.TypeChanged));
        if(comparisonType.IsPrimitive) return diff.ConditionalAddEntry(oldObject.Equals(newObject), new DBDifferenceEntry("", oldObject, newObject, DifferenceReason.ValueChanged));
        if(comparisonType.IsEnum) return diff.ConditionalAddEntry(oldObject.Equals(newObject), new DBDifferenceEntry("", oldObject, newObject, DifferenceReason.ValueChanged));
        if(comparisonType == typeof(string)) return diff.ConditionalAddEntry(oldObject.Equals(newObject), new DBDifferenceEntry("", oldObject, newObject, DifferenceReason.ValueChanged));
        if(comparisonType == typeof(DateTime)) return diff.ConditionalAddEntry(((DateTime)oldObject - (DateTime)newObject).Duration().TotalSeconds < 1, new DBDifferenceEntry("", oldObject, newObject, DifferenceReason.ValueChanged));
        // Compare lists
        if (comparisonType.IsGenericType && comparisonType.GetGenericTypeDefinition() == typeof(List<>))
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
        if(oldCount != newCount) return diff.AddEntry(new DBDifferenceEntry("Count", oldCount, newCount, DifferenceReason.ListLengthChanged));
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