namespace OculusDB.ObjectConverters;

public class DifferenceTypeIndex
{
    public static List<DifferenceTypeIndexEntry> differenceTypes { get; set; } = new List<DifferenceTypeIndexEntry>();

    public static void Init()
    {
        differenceTypes.Clear();
        foreach(int i in Enum.GetValues(typeof(DifferenceNameType))) {
            string name = Enum.GetName(typeof(DifferenceNameType), i) ?? "";
            differenceTypes.Add(new DifferenceTypeIndexEntry()
            {
                displayName = OculusConverter.FormatDBEnumString(name),
                enumName = name,
                value = i
            });
        }
    }

    public static string AllEnumNames()
    {
        return String.Join(",", differenceTypes.Select(x => x.enumName));
    }
    
    public static DifferenceNameType ParseEnum(string enumName)
    {
        DifferenceNameType type = DifferenceNameType.Unknown;
        if(!Enum.TryParse(enumName, true, out type)) return DifferenceNameType.Unknown;
        return type;
    }
}

public class DifferenceTypeIndexEntry
{
    public string displayName { get; set; } = "";
    public string enumName { get; set; } = "";
    public int value { get; set; } = 0;
}