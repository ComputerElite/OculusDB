namespace OculusGraphQLApiLib.Game;

public class FileSegment
{
    /// <summary>
    /// binary id of the version
    /// </summary>
    public string binaryId { get; set; } = "";
    /// <summary>
    /// SHA256 of the segment
    /// </summary>
    public string sha256 { get; set; } = "";
    /// <summary>
    /// Where the segment is saved after decompression
    /// </summary>
    public string tmpFileDestination { get; set; } = "";

    /// <summary>
    /// File name in the game files
    /// </summary>
    public string file { get; set; } = "";
    /// <summary>
    /// Amount of segments in the file
    /// </summary>
    public int segmentCount { get; set; } = 0;
}