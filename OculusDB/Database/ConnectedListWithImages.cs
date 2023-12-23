namespace OculusDB.Database;

public class ConnectedListWithImages : ConnectedList
{
    public List<DBAppImage> imgs { get; set; } = new ();
    public List<DBOffer> offers { get; set; } = new ();
}