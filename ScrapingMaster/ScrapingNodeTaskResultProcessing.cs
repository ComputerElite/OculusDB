namespace OculusDB.ScrapingMaster;

public class ScrapingNodeTaskResultProcessing
{
    public long processingCount { get; set; } = 0;

    public void Start()
    {
        processingCount++;
    }
    public void Done()
    {
        processingCount--;
    }

    public bool IsProcessing()
    {
        // Perhaps add a timeout to make sure that the processing is not stuck and thus never goes to false
        return processingCount > 0;
    }
}