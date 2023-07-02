namespace OculusDB.ScrapingMaster;

public enum ScrapingNodeStatus
{
    RequestingToDo,
    Scraping,
    Idling,
    TransmittingResults,
    StartingUp,
    Offline,
    WaitingForMasterServer
}