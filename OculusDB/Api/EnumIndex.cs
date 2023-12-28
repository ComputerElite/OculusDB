using OculusDB.ApiDocs;
using OculusDB.Database;
using OculusDB.ObjectConverters;
using OculusGraphQLApiLib.Results;

namespace OculusDB.Api;

public class EnumIndex
{
    public static List<EnumEntry> differenceNameTypes = new List<EnumEntry>();
    public static List<EnumEntry> searchEntryTypes = new List<EnumEntry>();
    public static List<EnumEntry> categoryTypes = new List<EnumEntry>();
    public static List<EnumEntry> genres = new List<EnumEntry>();

    public static void Init()
    {
        PopulateEnum(ref differenceNameTypes, typeof(DifferenceNameType));
        PopulateEnum(ref categoryTypes, typeof(Category), true);
        PopulateEnum(ref genres, typeof(Genre), true);
        searchEntryTypes = new List<EnumEntry>
        {
            new EnumEntry("Applications", DBDataTypes.Application),
            new EnumEntry("Dlcs", DBDataTypes.IapItem),
            new EnumEntry("Dlc packs", DBDataTypes.IapItemPack),
            new EnumEntry("Achievements", DBDataTypes.Achievement)
        };
    }

    public static void PopulateEnum(ref List<EnumEntry> list, Type enumType, bool isOculusType = false)
    {
        list.Clear();
        foreach(int i in Enum.GetValues(enumType)) {
            string name = Enum.GetName(enumType, i) ?? "";
            list.Add(new EnumEntry()
            {
                displayName = isOculusType ? OculusConverter.FormatOculusEnumString(name) : OculusConverter.FormatDBEnumString(name),
                enumName = name,
                value = i
            });
        }
    }

    public static string AllEnumNamesDifferenceNameTypes()
    {
        return String.Join(",", differenceNameTypes.Select(x => x.enumName));
    }
    
    public static DifferenceNameType parseEnumDifferenceNameType(string enumName)
    {
        return ParseDBEnum<DifferenceNameType>(enumName);
    }
    
    public static T ParseDBEnum<T>(string enumName) where T : Enum
    {
        // Check if enumName is present in enum
        if (!Enum.IsDefined(typeof(T), enumName)) return (T)(object)-1;
        T type = (T)Enum.Parse(typeof(T), enumName, true);
        return type;
    }
}

