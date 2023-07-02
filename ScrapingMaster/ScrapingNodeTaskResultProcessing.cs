namespace OculusDB.ScrapingMaster;

public class ScrapingNodeTaskResultProcessing
{
    public bool processing { get; set; } = false;
    public DateTime processingDone { get; set; } = DateTime.MinValue;

    public DateTime processingStart { get; set; } = DateTime.MinValue;

    public void Start()
    {
        processingStart = DateTime.UtcNow;
        processing = true;
    }
    public void Done()
    {
        processingDone = DateTime.UtcNow;
        processing = false;
    }

    public bool IsProcessing()
    {
        // Perhaps add a timeout to make sure that the processing is not stuck and thus never goes to false
        return processing;
    }
}