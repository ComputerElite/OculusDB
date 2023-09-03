using ComputerUtils.ConsoleUi;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using ComputerUtils.VarUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Game
{
    public class GameDownloader
    {
        public static string customManifestError = "";
        public static bool ignoreErrors = false;
        public static void Decompress(Stream input, string dest)
        {
            Ionic.Zlib.DeflateStream s = new Ionic.Zlib.DeflateStream(input, Ionic.Zlib.CompressionMode.Decompress);
            FileStream res = File.Open(dest, FileMode.Append);
            s.CopyTo(res);
            s.Close();
            res.Close();
            res.Dispose();
            return;
        }

        public static bool DownloadRiftGame(string destination, string access_token, string binaryId)
        {
            FileManager.CreateDirectoryIfNotExisting(AppDomain.CurrentDomain.BaseDirectory + "tmp");
            if (!destination.EndsWith(Path.DirectorySeparatorChar.ToString())) destination += Path.DirectorySeparatorChar;
            string manifestPath = destination +  "manifest.json";
            DownloadManifest(manifestPath, access_token, binaryId);
            if(!File.Exists(manifestPath))
            {
                Logger.Log("Manifest does not exist");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Manifest does not exist");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
            Console.WriteLine();
            Manifest manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath));
            ProgressBarUI totalProgress = new ProgressBarUI();
            totalProgress.Start();
            totalProgress.eTARange = 20;
            DownloadProgressUI segmentDownloader = new DownloadProgressUI();
            segmentDownloader.connections = 5;
            long done = 0;
            Logger.notAllowedStrings.Add(access_token);
            long total = 0;
            foreach (KeyValuePair<string, ManifestFile> f in manifest.files) total += f.Value.size;
            totalProgress.UpdateProgress(done, total, SizeConverter.ByteSizeToString(done), SizeConverter.ByteSizeToString(total), "", true);
            foreach (KeyValuePair<string, ManifestFile> f in manifest.files)
            {

                string fileDest = destination + f.Key.Replace('/', Path.DirectorySeparatorChar);
                Console.WriteLine();
                if (DownloadFile(f.Value, fileDest, access_token, binaryId, segmentDownloader))
                {
                    done += new FileInfo(fileDest).Length;
                }
                totalProgress.UpdateProgress(done, total, SizeConverter.ByteSizeToString(done), SizeConverter.ByteSizeToString(total), "", true);
                Console.WriteLine();
            }
            Console.ForegroundColor = ConsoleColor.White;
            return Validator.ValidateGameInstall(destination, manifestPath);
        }
        
        public static bool DownloadRiftGameParallel(string destination, string access_token, string binaryId)
        {
            // make sure access token doesn't get logged
            Logger.notAllowedStrings.Add(access_token);
            
            // Create tmp directory
            FileManager.CreateDirectoryIfNotExisting(AppDomain.CurrentDomain.BaseDirectory + "tmp");
            // Add slash to file path if it ain't there
            if (!destination.EndsWith(Path.DirectorySeparatorChar.ToString())) destination += Path.DirectorySeparatorChar;
            string manifestPath = destination +  "manifest.json";
            // Download manifest of version
            DownloadManifest(manifestPath, access_token, binaryId);
            if(!File.Exists(manifestPath))
            {
                // on download error return
                Logger.Log("Manifest does not exist");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Manifest does not exist");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
            Console.WriteLine();
            
            // load manifest of version
            Manifest manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath));
            
            long total = 0;
            
            Logger.Log("Preparing download. Converting manifest into list of segments to download");
            Console.WriteLine("Preparing download");
            
            List<FileSegment> segmentsToDownload = new List<FileSegment>();
            foreach (KeyValuePair<string, ManifestFile> f in manifest.files)
            {
                total += f.Value.size;
                foreach (object[] segment in f.Value.segments)
                {
                    FileSegment seg = new FileSegment();
                    seg.sha256 = segment[1].ToString();
                    seg.binaryId = binaryId;
                    seg.tmpFileDestination = AppDomain.CurrentDomain.BaseDirectory + "tmp" + Path.DirectorySeparatorChar + seg.sha256;
                    seg.segmentCount = f.Value.segments.Length;
                    seg.file = f.Key;
                    segmentsToDownload.Add(seg);
                }
            }
            
            DownloadSegments(total, segmentsToDownload, access_token, manifest, destination, true);
            
            return Validator.ValidateGameInstall(destination, manifestPath, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="total"></param>
        /// <param name="segmentsToDownload"></param>
        /// <param name="access_token"></param>
        /// <param name="manifest"></param>
        /// <param name="destination"></param>
        /// <param name="announceMissingFiles"></param>
        /// <returns>Successfully processed files</returns>
        public static int DownloadSegments(long total, List<FileSegment> segmentsToDownload, string access_token, Manifest manifest, string destination, bool announceMissingFiles = true)
        {
            const int maxParallelDownloads = 7;
            long done = 0;
            // Download segments. Save as sha256 in tmp directory
            Logger.Log("Downloading segments");
            Console.WriteLine("Downloading segments");
            
            Console.WriteLine();
            Console.WriteLine("Total Progress");
            ProgressBarUI totalProgress = new ProgressBarUI();
            totalProgress.Start();
            totalProgress.eTARange = 20; // use last 20 progress reports for eta
            totalProgress.UpdateProgress(done, total, SizeConverter.ByteSizeToString(done), SizeConverter.ByteSizeToString(total), "", true, true);
            Console.WriteLine();
            
            List<FileSegment> queuedSegments = new List<FileSegment>(segmentsToDownload);
            List<FileSegment> downloadedSegments = new List<FileSegment>();

            // Create Segment downloaders
            List<SegmentDownloader> segmentDownloaders = new List<SegmentDownloader>();
            for(int i = 0; i < maxParallelDownloads; i++)
            {
                SegmentDownloader segmentDownloader = new SegmentDownloader();
                segmentDownloader.access_token = access_token;
                segmentDownloader.progressUI = new ProgressBarUI();
                if (queuedSegments.Count >= 1)
                {
                    segmentDownloader.AddToDownloadQueue(queuedSegments[0]);
                    queuedSegments.RemoveAt(0);
                }
                segmentDownloaders.Add(segmentDownloader);
            }

            DateTime lastProgressDisplayUpdate = DateTime.MinValue;

            // Update download progress
            while (downloadedSegments.Count < segmentsToDownload.Count)
            {
                done = 0;
                // Update progress and download queue for all segment downloaders
                for (int i = 0; i < segmentDownloaders.Count; i++)
                {
                    // Add segment to queue if there are segments left to download
                    while (segmentDownloaders[i].downloadQueue.Count < 2 && queuedSegments.Count >= 1)
                    {
                        segmentDownloaders[i].AddToDownloadQueue(queuedSegments[0]);
                        queuedSegments.RemoveAt(0);
                    }
                    
                    // Make sure download is running
                    segmentDownloaders[i].MakeSureDownloadIsRunning();
                    
                    // Add downloaded segments of downloader to downloadedSegments
                    while (segmentDownloaders[i].downloadedFiles.Count > 0)
                    {
                        FileSegment downloaded = segmentDownloaders[i].downloadedFiles[0];
                        if(downloaded == null) continue;
                        downloadedSegments.Add(downloaded);
                        segmentDownloaders[i].downloadedFiles.RemoveAt(segmentDownloaders[i].downloadedFiles.FindIndex(x => x.sha256 == downloaded.sha256));
                    }

                    done += segmentDownloaders[i].totalDownloadedBytes;
                }

                bool shouldUpdateDisplayedProgress = (DateTime.Now - lastProgressDisplayUpdate).TotalMilliseconds > 100;
                if (shouldUpdateDisplayedProgress)
                {
                    ShowProgress(ref lastProgressDisplayUpdate, ref totalProgress, ref done, ref total, ref segmentDownloaders, ref downloadedSegments, ref segmentsToDownload);
                }
            }
            ShowProgress(ref lastProgressDisplayUpdate, ref totalProgress, ref done, ref total, ref segmentDownloaders, ref downloadedSegments, ref segmentsToDownload);

            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Logger.Log("Download finished, moving downloaded files to correct destination");
            Console.WriteLine("Download finished. Processing files...");
            int goodFiles = 0;
            int badFiles = 0;
            int totalFiles = manifest.files.Count;
            
            foreach (KeyValuePair<string, ManifestFile> f in manifest.files)
            {
                // Check if file should be processed
                
                List<string> segmentsSHA256 = new List<string>();
                foreach (object[] segment in f.Value.segments)
                {
                    segmentsSHA256.Add(segment[1].ToString());
                }

                if (!downloadedSegments.Any(x => segmentsSHA256.Contains(x.sha256)))
                {
                    // No downloaded segment contains this file. Skip it
                    Logger.Log("Skipping " + f.Key + " as no downloaded segment is part of it (" + SizeConverter.ByteSizeToString(f.Value.size) + ")");
                    totalFiles--;
                    continue;
                }
                
                Console.WriteLine("Processing " + f.Key + " (" + SizeConverter.ByteSizeToString(f.Value.size) + ")");
                string fileDestination = destination + f.Key.Replace('/', Path.DirectorySeparatorChar);
                FileManager.CreateDirectoryIfNotExisting(Directory.GetParent(fileDestination).FullName);
                FileStream fileStream = File.Open(fileDestination, FileMode.Create);
                bool success = true;
                foreach (object[] segment in f.Value.segments)
                {
                    if (!downloadedSegments.Any(x => x.sha256 == segment[1].ToString()))
                    {
                        success = false;
                        break;
                    }
                    
                    FileSegment s = downloadedSegments.FirstOrDefault(x => x.sha256 == segment[1].ToString());
                    if(!File.Exists(s.tmpFileDestination))
                    {
                        if (announceMissingFiles)
                        {
                            Logger.Log("Segment " + s.tmpFileDestination + " does not exist");
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("File downloaded incompletely");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        success = false;
                        break;
                    }
                    // Read segment and append it to the final file
                    FileStream segmentStream = File.Open(s.tmpFileDestination, FileMode.Open);
                    segmentStream.CopyTo(fileStream);
                    segmentStream.Close();
                    segmentStream.Dispose();
                    
                    // remove 1 occurance of segment
                    downloadedSegments.RemoveAt(downloadedSegments.FindIndex(x => x.sha256 == segment[1].ToString()));
                    // Delete the downloaded segment if it isn't needed anymore
                    if(!downloadedSegments.Any(x => x.sha256 == segment[1].ToString())) File.Delete(s.tmpFileDestination);
                }
                fileStream.Close();
                fileStream.Dispose();

                if (!success)
                {
                    if (announceMissingFiles)
                    {
                        Logger.Log("Segment of " + f.Key + " missing");
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("File downloaded incompletely. Please validate your game files.");
                        Console.ForegroundColor = ConsoleColor.White;
                        badFiles++;
                    }
                }
                else
                {
                    Logger.Log("Processed " + f.Key + " successfully");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("File processed successfully");
                    Console.ForegroundColor = ConsoleColor.White;
                    goodFiles++;
                }
            }
            
            Console.WriteLine("Processed " + goodFiles + " / " + totalFiles + " files successfully");

            Console.ForegroundColor = ConsoleColor.White;
            return goodFiles;
        }

        public static void ShowProgress(ref DateTime lastProgressDisplayUpdate, ref ProgressBarUI totalProgress, ref long done, ref long total, ref List<SegmentDownloader> segmentDownloaders, ref List<FileSegment> downloadedSegments, ref List<FileSegment> segmentsToDownload)
        {
            lastProgressDisplayUpdate = DateTime.Now;
            //Console.WriteLine("Total Progress");
            totalProgress.UpdateProgress(done, total, SizeConverter.ByteSizeToString(done), SizeConverter.ByteSizeToString(total), downloadedSegments.Count + " / " + segmentsToDownload.Count + " segments downloaded", true, true);
            ConsoleUiController.WriteEmptyLine();
            ConsoleUiController.WriteEmptyLine("Download jobs");
            for (int i = 0; i < segmentDownloaders.Count; i++)
            {
                segmentDownloaders[i].ShowProgress();
            }

            ConsoleUiController.WriteEmptyLine();
            ConsoleUiController.WriteEmptyLine();
            ConsoleUiController.WriteEmptyLine();
        }

        public static string ConstructSegmentDownloadUrl(string binaryId, string sha256, string access_token)
        {
            return  "https://securecdn.oculus.com/binaries/segment/?access_token=" + access_token + "&binary_id=" + binaryId + "&segment_sha256=" + sha256;
        }

        public static bool DownloadFile(ManifestFile file, string fileDest, string access_token, string binaryId, DownloadProgressUI downloadProgressUI = null)
        {
            if(!Logger.notAllowedStrings.Contains(access_token)) Logger.notAllowedStrings.Add(access_token);
            if (downloadProgressUI == null) downloadProgressUI = new DownloadProgressUI();
            if (File.Exists(fileDest)) File.Delete(fileDest);
            FileManager.CreateDirectoryIfNotExisting(FileManager.GetParentDirIfExisting(fileDest));
            int done = 0;
            foreach (object[] segment in file.segments)
			{
                done++;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Downloading file segment " + done + " / " + file.segments.Length);
                string url = ConstructSegmentDownloadUrl(binaryId, segment[1].ToString(), access_token);
                bool downloaded = false;
                const int maxAttempts = 5;
                for (int i = 0; i < maxAttempts; i++)
                {
                    if(i > 0) Console.WriteLine("Download of segment failed. Retrying... (" + (i + 1) + "/" + maxAttempts + ")");
                    if (!downloadProgressUI.StartDownload(url,
                            AppDomain.CurrentDomain.BaseDirectory + "tmp" + Path.DirectorySeparatorChar + "file", true,
                            true, new Dictionary<string, string> { { "User-Agent", Constants.UA } }))
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    Stream s = File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "tmp" + Path.DirectorySeparatorChar + "file");
                    s.ReadByte();
                    s.ReadByte();
                    Decompress(s, fileDest);
                    s.Close();
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + "tmp" + Path.DirectorySeparatorChar + "file");
                    downloaded = true;
                    break;
                }

                if (!downloaded)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Download of segment failed.");
                    return false;
                }
            }
            return true;
        }

        public static bool DownloadManifest(string destination, string access_token, string binaryId)
        {
            string baseDownloadLink = "https://securecdn.oculus.com/binaries/download/?id=" + binaryId + "&access_token=" + access_token;
            FileManager.CreateDirectoryIfNotExisting(FileManager.GetParentDirIfExisting(destination));
            if (File.Exists(destination)) File.Delete(destination);
            Logger.Log("Downloading manifest of " + binaryId);
            Console.WriteLine("Downloading manifest");
            DownloadProgressUI progressUI = new DownloadProgressUI();
            Logger.Log("Downloading manifest");
            Logger.notAllowedStrings.Add(access_token);
            if(!progressUI.StartDownload(baseDownloadLink + "&get_manifest=1", destination + ".zip"))
            {
                Logger.Log("Download of manifest failed. Aborting.", LoggingType.Warning);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine("Download of manifest failed.\n\nDo you own this game?\n\n-If you do, check if you got the right headset selected in the main menu.\n- If that's the case update your access_token in case it's expired.\n\n " + customManifestError);
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
            ZipArchive a = ZipFile.OpenRead(destination + ".zip");
            foreach(ZipArchiveEntry e in a.Entries)
            {
                if(e.Name.EndsWith(".json"))
                {
                    e.ExtractToFile(destination);
                }
            }
            if(!File.Exists(destination))
            {
                Logger.Log("Manifest download failed");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Manifest download failed");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
            Logger.Log("Download of Manifest succeeded");
            Console.ForegroundColor= ConsoleColor.Green;
            Console.WriteLine("Download of Manifest succeeded");
            Console.ForegroundColor = ConsoleColor.White;
            return true;
        }

        public static bool DownloadGearVRGame(string destination, string access_token, string binaryId)
        {
            return DownloadMontereyGame(destination, access_token, binaryId);
        }

        public static bool DownloadPacificGame(string destination, string access_token, string binaryId)
        {
            return DownloadMontereyGame(destination, access_token, binaryId);
        }

        public static bool DownloadMontereyGame(string destination, string access_token, string binaryId)
        {
            string baseDownloadLink = "https://securecdn.oculus.com/binaries/download/?id=" + binaryId + "&access_token=" + access_token;
            Logger.Log("Starting download of " + binaryId);
            Console.WriteLine("Starting download of " + binaryId);
            DownloadProgressUI ui = new DownloadProgressUI();
            ui.connections = 10;
            Logger.notAllowedStrings.Add(access_token);
            if(!ui.StartDownload(baseDownloadLink, destination, true, true, new Dictionary<string, string> { { "User-Agent", Constants.UA } }))
            {
                return false;
            }
            Logger.Log("Download finished");
            return File.Exists(destination);
        }

        public static bool DownloadObbFiles(string destinationDir, string access_token, List<Obb> obbs)
        {
            Logger.Log("Downloading " + obbs.Count + " obb files");
            FileManager.CreateDirectoryIfNotExisting(destinationDir);
            ProgressBarUI totalProgress = new ProgressBarUI();
            totalProgress.Start();
            totalProgress.eTARange = 20;
            DownloadProgressUI segmentDownloader = new DownloadProgressUI();
            segmentDownloader.connections = 10;
            long done = 0;
            long doneFiles = 0;
            Logger.notAllowedStrings.Add(access_token);
            long total = 0;
            long totalFiles = 0;
            foreach (Obb f in obbs) total += f.bytes;
            totalFiles = obbs.Count;
            List<KeyValuePair<DateTime, long>> lastBytes = new List<KeyValuePair<DateTime, long>>();
            totalProgress.UpdateProgress(done, total, doneFiles + " files (" + SizeConverter.ByteSizeToString(done) + ")", totalFiles + " files (" + SizeConverter.ByteSizeToString(total) + ")", "", true);
            foreach (Obb f in obbs)
            {
                string fileDest = destinationDir + f.filename;
                Console.WriteLine();
                segmentDownloader.StartDownload("https://securecdn.oculus.com/binaries/download/?id=" + f.id + "&access_token=" + access_token, fileDest, true, true, new Dictionary<string, string> { { "User-Agent", Constants.UA } });
                done += new FileInfo(fileDest).Length;
                doneFiles++;
                totalProgress.UpdateProgress(done, total, doneFiles + " files (" + SizeConverter.ByteSizeToString(done) + ")", totalFiles + " files (" + SizeConverter.ByteSizeToString(total) + ")", "", true);
            }
            return true;
        }
    }

    public class Obb
    {
        public string id { get; set; } = "";
        public string filename { get; set; } = "";
        public long bytes { get; set; } = 0;
    }
}
