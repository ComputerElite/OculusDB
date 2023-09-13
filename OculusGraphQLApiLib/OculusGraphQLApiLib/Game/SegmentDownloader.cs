using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using ComputerUtils;
using ComputerUtils.ConsoleUi;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;

namespace OculusGraphQLApiLib.Game;

public class SegmentDownloader
{
    public List<FileSegment> downloadQueue { get; set; } = new List<FileSegment>();
    public FileSegment currentlyDownloading { get; set; } = new FileSegment();
    public List<FileSegment> downloadedFiles { get; set; } = new List<FileSegment>();
    
    public long downloadedBytes { get; set; } = 0;
    public long totalBytes { get; set; } = 0;
    public long totalDownloadedBytes { get; set; } = 0;
    public const string UserAgent = "OculusGraphQLApiLib/1.0";
    private long finishedFilesTotalDownloadedBytes { get; set; } = 0;
    SHA256 shaCalculator = SHA256.Create();
    
    public string access_token { get; set; } = "";
    bool downloading = false;
    public ProgressBarUI progressUI;

    public string extraText = "";

    private int downloadAttempt = 0;

    public void DownloadNextFileFromQueue()
    {
        if (downloadQueue.Count <= 0)
        {
            downloading = false;
            extraText = "Downloads done!";
            return;
        }

        downloading = true;
        currentlyDownloading = downloadQueue[0];
        downloadQueue.RemoveAt(0);

        extraText = "Checking for " + currentlyDownloading.file;
        
        if (File.Exists(currentlyDownloading.tmpFileDestination))
        {
            // Calculate hash and check if it matches
            FileStream stream = File.OpenRead(currentlyDownloading.tmpFileDestination);
            if (BitConverter.ToString(shaCalculator.ComputeHash(stream)).Replace("-", "").ToLower() == currentlyDownloading.sha256.ToLower())
            {
                // Hash matches, no need to download
                stream.Close();
                stream.Dispose();
                AfterFileDecompressed();
                return;
            }
            else
            {
                // Hash doesn't match, delete file
                stream.Close();
                stream.Dispose();
                File.Delete(currentlyDownloading.tmpFileDestination);
            }
        }
        extraText = "Downloading " + currentlyDownloading.file;

        ComputerUtils_FastFileDownloader.FileDownloader downloader = new ComputerUtils_FastFileDownloader.FileDownloader();
        downloader.OnDownloadProgress = () =>
        {
            downloadedBytes = downloader.downloadedBytes;
            totalBytes = downloader.totalBytes;
            totalDownloadedBytes = finishedFilesTotalDownloadedBytes + downloader.downloadedBytes;
        };
        downloader.OnDownloadComplete = () =>
        {
            downloadAttempt = 1;
            // decompress downloaded file
            extraText = "Decompressing " + currentlyDownloading.file;
            Stream s = File.OpenRead(currentlyDownloading.tmpFileDestination + ".tmp");
            s.ReadByte();
            s.ReadByte();
            GameDownloader.Decompress(s, currentlyDownloading.tmpFileDestination);
            s.Close();
            File.Delete(currentlyDownloading.tmpFileDestination + ".tmp");
            AfterFileDecompressed();

        };
        downloader.OnDownloadError = () =>
        {
            if (downloadAttempt >= 3)
            {
                // Download failed too often. Abort.
                downloadAttempt = 1;
                Logger.Log("Download of " + currentlyDownloading.file + " (" + currentlyDownloading.sha256 + ") failed. Max retries reached.", LoggingType.Error);
                downloadedFiles.Add(currentlyDownloading);
            }
            else
            {
                downloadedBytes = 0;
                totalDownloadedBytes = 0;
                totalBytes = 0;
                downloadAttempt++;
                Logger.Log("Download of " + currentlyDownloading.file + " (" + currentlyDownloading.sha256 + ") failed. Retrying... Attempt " + downloadAttempt, LoggingType.Warning);
                // Add download back to queue to retry.
                downloadQueue.Insert(0, currentlyDownloading);
            }
            currentlyDownloading = new FileSegment();
            DownloadNextFileFromQueue();
        };
        downloader.DownloadFile(GameDownloader.ConstructSegmentDownloadUrl(currentlyDownloading.binaryId, currentlyDownloading.sha256, access_token), currentlyDownloading.tmpFileDestination + ".tmp", 1);
    }

    public void AfterFileDecompressed()
    {
        finishedFilesTotalDownloadedBytes += new FileInfo(currentlyDownloading.tmpFileDestination).Length;
        totalDownloadedBytes = finishedFilesTotalDownloadedBytes;
            
        // mark file as downloaded
        downloadedFiles.Add(currentlyDownloading);
        currentlyDownloading = new FileSegment();
        DownloadNextFileFromQueue();
    }

    public void MakeSureDownloadIsRunning()
    {
        if(!downloading && downloadQueue.Count > 0) StartDownload();
    }
    
    public void AddToDownloadQueue(FileSegment segment)
    {
        downloadQueue.Add(segment);
    }
    
    public void StartDownload()
    {
        DownloadNextFileFromQueue();
    }

    public void ShowProgress()
    {
        progressUI.Start();
        progressUI.UpdateProgress(downloadedBytes, totalBytes, SizeConverter.ByteSizeToString(downloadedBytes), SizeConverter.ByteSizeToString(totalBytes), extraText);
    }
}
