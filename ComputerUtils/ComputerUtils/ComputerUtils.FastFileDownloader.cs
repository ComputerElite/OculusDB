using System.Net;
using ComputerUtils.Logging;

namespace ComputerUtils;

public class ComputerUtils_FastFileDownloader
{
    public class FileJunkDownloader
    {
        private readonly string url;
        private readonly int length;
        private readonly int start;

        public FileJunkDownloader(string url, int start, int length)
        {
            this.url = url;
            this.start = start;
            this.length = length;
        }

        public void DownloadFilePart()
        {
            
        }
    }

    public class FileDownloader
    {
            public long downloadedBytes = 0;
            public long totalBytes = 0;
            public bool error = false;
            public Exception exception;
            public bool canceled = false;
            public Action OnDownloadComplete;
            public Action OnDownloadProgress;
            public Action OnDownloadError;
            public Dictionary<string, string> headers = new Dictionary<string, string>();
            public string UserAgent = "ComputerUtils.FastFileDownloader/1.0";

            public void DownloadFile(string url, string savePath, int numConnections)
            {
                Thread t = new Thread(() =>
                {
                    DownloadFileInternal(url, savePath, numConnections);
                });
                t.Start();
            }

            public void DownloadFileInternal(string url, string savePath, int numConnections)
            {
                // Create an array to store the downloaded bytes for each connection
                long[] bytesDownloadedArray = new long[numConnections];
                long chunkSize;

                long[] startPosArray;
                long[] endPosArray;
                long fileSize = -1;
                if (numConnections > 1)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "GET";
                    request.UserAgent = UserAgent;
                    request.AllowAutoRedirect = true;
                    try
                    {
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        fileSize = response.ContentLength;
                        response.Close();
                    } catch (Exception e)
                    {
                        Logger.Log("Error while initial GET request for file size: " + e);
                        error = true;
                        exception = e;
                        OnDownloadError?.Invoke();
                        return;
                    }

                    if (fileSize <= 0)
                    {
                        
                        Logger.Log("File size is " + fileSize + ". Thus we cannot download the file");
                        error = true;
                        exception = new Exception("File size is 0");
                        OnDownloadError?.Invoke();
                        return;
                    }
                    totalBytes = fileSize;
            
                    Logger.Log("File size: " + fileSize);
            
                    chunkSize = fileSize / numConnections;
            
                    startPosArray = new long[numConnections];
                    endPosArray = new long[numConnections];
            
                    for (int i = 0; i < numConnections; i++)
                    {
                        startPosArray[i] = i * chunkSize;
                        endPosArray[i] = (i + 1) * chunkSize - 1;
            
                        if (i == numConnections - 1) endPosArray[i] = fileSize - 1;
            
                        // Console.WriteLine("Connection " + i + " range: " + startPosArray[i] + "-" + endPosArray[i]);
                    }
                }
                else
                {
                    startPosArray = new long[] { -1 };
                    endPosArray = new long[] { -1 };
                }
                
            
            
                // Download each chunk in a separate thread
                for (int i = 0; i < numConnections; i++)
                {
                    int chunkIndex = i;
                    new Thread(() =>
                    {
                        fileSize = DownloadChunk(url, savePath, startPosArray[chunkIndex], endPosArray[chunkIndex], chunkIndex, ref bytesDownloadedArray);
                    }).Start();
                }
                    
                // Wait for all threads to complete
                while (true)
                {
                    downloadedBytes = 0;
                    for (int i = 0; i < numConnections; i++)
                    {
                        downloadedBytes += bytesDownloadedArray[i];
                    }

                    if (canceled || error) return;
        
                    //double progress = (double)downloadedBytes / fileSize * 100;
                    //Logger.Log("Download progress: " + progress.ToString("0.00") + "%");
                    OnDownloadProgress.Invoke();
        
                    if (downloadedBytes == fileSize) break;
                    Thread.Sleep(200);
                }
                
        
               
        
               Logger.Log("Download complete!");

               if (numConnections > 1)
               {
                   // Merge downloaded chunks into a single file
                   using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                   {
                       byte[] buffer = new byte[1024 * 1024 * 20]; // 20MB buffer

                       for (int i = 0; i < numConnections; i++)
                       {
                           using (FileStream chunkStream = new FileStream(savePath + "." + i, FileMode.Open, FileAccess.Read))
                           {
                               int bytesRead;
                               while ((bytesRead = chunkStream.Read(buffer, 0, buffer.Length)) > 0)
                               {
                                   fileStream.Write(buffer, 0, bytesRead);
                               }
                           }

                           File.Delete(savePath + "." + i);
                       }
                   }
               }
                

                Logger.Log("File saved at " + savePath);
                OnDownloadComplete?.Invoke();
            }

            public long DownloadChunk(string url, string savePath, long startPos, long endPos, int chunkIndex, ref long[] bytesDownloadedArray)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.UserAgent = UserAgent;
                foreach(KeyValuePair<string, string> header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
                if(startPos != -1 && endPos != -1) request.AddRange(startPos, endPos);
                long totalBytesRead = 0;
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if(endPos == -1) totalBytes = response.ContentLength;
                    Stream stream = response.GetResponseStream();

                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    
                    string filePath = endPos == -1 ? savePath : savePath + "." + chunkIndex;

                    using (FileStream fileStream =
                           new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            if (error || canceled)
                            {
                                stream.Close();
                                response.Close();
                                return totalBytesRead;
                            }
                            fileStream.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            bytesDownloadedArray[chunkIndex] = totalBytesRead;
                        }
                    }

                    stream.Close();
                    response.Close();

                    Logger.Log("Chunk " + chunkIndex + " download complete!");
                }
                catch (Exception e)
                {
                    Logger.Log("Error while downloading file chunk " + chunkIndex + ": " + e);
                    Cancel();
                    error = true;
                    exception = e;
                    OnDownloadError.Invoke();
                }

                return totalBytesRead;
            }

            public void Cancel()
            {
                canceled = true;
            }
    }
}