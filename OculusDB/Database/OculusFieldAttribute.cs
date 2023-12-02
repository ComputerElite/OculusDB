public class OculusField : Attribute
{
    public string fieldName;

    public OculusField(string name)
    {
        fieldName = name;
    }
}

public class OculusFieldAlternate : Attribute
{
    public string fieldName;

    public OculusFieldAlternate(string name)
    {
        fieldName = name;
    }
}