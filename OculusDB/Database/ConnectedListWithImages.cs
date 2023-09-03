namespace OculusDB.Database;

public class ConnectedListWithImages : ConnectedList
{
    public List<DBAppImage> imgs { get; set; } = new ();
}