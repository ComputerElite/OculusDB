using ComputerUtils.ConsoleUi;
using ComputerUtils.FileManaging;
using ComputerUtils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace ComputerUtils.Updating
{
    public class Updater
    {
        public string version = "1.0.0";
        public string exe = AppDomain.CurrentDomain.BaseDirectory;
        public string exeName = "";
        public string AppName = "";
        public string GitHubRepoLink = "";
        public string exeLocation = "";

        public Updater(string currentVersion, string GitHubRepoLink, string AppName, string exeLocation, string exeName = "auto")
        {
            this.version = currentVersion;
            this.GitHubRepoLink = GitHubRepoLink;
            this.AppName = AppName;
            this.exeName = exeName;
            this.exeLocation = exeLocation;
        }

        public Updater() { }

        public bool CheckUpdate()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Checking for updates");
            GithubRelease latest = GetLatestVersion();
            if (latest.comparedToCurrentVersion == 1)
            {
                Logger.Log("Update available: " + version + " -> " + latest.tag_name);
                Console.WriteLine("New update availabel! Current version: " + version + ", latest version: " + latest.tag_name);
                return true;
            }
            else if (latest.comparedToCurrentVersion == -2)
            {
                Logger.Log("Error while checking for updates", LoggingType.Error);
                Console.WriteLine("An Error occured while checking for updates");
            }
            else if (latest.comparedToCurrentVersion == -1)
            {
                Logger.Log("User on preview version: " + version + " Latest stable: " + latest.tag_name);
                Console.WriteLine("Have fun on a preview version (" + version + "). You can downgrade to the latest stable release (" + latest.tag_name + ") by pressing enter.");
                return true;
            }
            else
            {
                Logger.Log("User on newest version");
                Console.WriteLine("You are on the newest version");
            }
            return false;
        }

        public GithubRelease GetLatestVersion()
        {
            try
            {
                Logger.Log("Fetching newest version");
                WebClient c = new WebClient();
                c.Headers.Add("user-agent", AppName + "/" + version);
                string repoApi = "https://api.github.com/repos/" + GitHubRepoLink.Split('/')[3] + "/" + GitHubRepoLink.Split('/')[4] + "/releases";
                string json = c.DownloadString(repoApi);
                //Logger.Log("GH API says: " + json);
                
                List<GithubRelease> updates = JsonSerializer.Deserialize<List<GithubRelease>>(json);

                GithubRelease latest = updates[0];
                latest.comparedToCurrentVersion = latest.GetVersion().CompareTo(new System.Version(version));
                return latest;
            }
            catch
            {
                Logger.Log("Fetching of newest version failed", LoggingType.Error);
                return new GithubRelease();
            }
        }

        /// <summary>
        /// Checks for updates, aks the user if they want to update, íf they want to it exits the program and starts the updating process
        /// </summary>
        public void UpdateAssistant()
        {
            if(CheckUpdate())
            {
                Logger.Log("Update available. Asking user if they want to update");
                string choice = ConsoleUiController.QuestionString("Do you want to update? (Y/n): ");
                if (choice.ToLower() == "y" || choice == "")
                {
                    StartUpdate(); // This function will exit the program
                }
                Logger.Log("Not updating.");
            }
        }

        public void Update()
        {
            Console.WriteLine(AppName + " started in update mode. Fetching newest version");
            GithubRelease e = GetLatestVersion();
            Console.WriteLine("Updating to version " + e.tag_name + ". Starting download (this may take a few seconds)");
            WebClient c = new WebClient();
            Logger.Log("Downloading update");
            c.DownloadFile(e.GetDownload(), exe + "update.zip");
            Logger.Log("Unpacking");
            Console.WriteLine("Unpacking update");
            string destDir = new DirectoryInfo(Path.GetDirectoryName(exe)).Parent.FullName + Path.DirectorySeparatorChar;
            string launchableExe = "";
            using (ZipArchive archive = ZipFile.OpenRead(exe + "update.zip"))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    String name = entry.FullName;
                    if (name.EndsWith(".exe")) launchableExe = name;
                    if (name.EndsWith("/")) continue;
                    if (name.Contains("/")) Directory.CreateDirectory(destDir + System.IO.Path.GetDirectoryName(name));
                    entry.ExtractToFile(destDir + entry.FullName, true);
                }
            }
            if(exeName != "auto")
            {
                launchableExe = exeName;
            }
            File.Delete(exe + "update.zip");
            Logger.Log("Update successful");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Updated to version " + e.tag_name + ". Changelog:\n" + e.body + "\n\nStart " + AppName + " by pressing any key");
            Console.ReadKey();
            Process.Start(destDir + launchableExe);
            Environment.Exit(0);
        }

        public void StartUpdate()
        {
            try
            {
                Logger.Log("Duplicating exe for update");
                Console.WriteLine("Duplicating required files");
                FileManager.DeleteDirectoryIfExisting(exe + "updater");
                Directory.CreateDirectory(exe + "updater");
                foreach (string f in Directory.GetFiles(exe))
                {
                    File.Copy(f, exe + "updater" + Path.DirectorySeparatorChar + Path.GetFileName(f), true);
                }
                foreach (string f in Directory.GetDirectories(exe))
                {
                    if (!f.EndsWith("runtimes") && !f.EndsWith("ref")) continue; // directories required by NET 6
                    FileManager.DirectoryCopy(f, exe + "updater" + Path.DirectorySeparatorChar + Path.GetFileName(f), true);
                }
                string toStart = exe + "updater" + Path.DirectorySeparatorChar + Path.GetFileName(exeLocation);
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) toStart = toStart.Replace(".dll", ".exe");
                Logger.Log("Starting update. Closing program");
                Console.WriteLine("Starting update.");
                Process.Start(new ProcessStartInfo
                {
                    FileName = toStart,
                    Arguments = "--update",
                    WorkingDirectory = Path.GetDirectoryName(exeLocation)
                });
                Environment.Exit(0);
            } catch(Exception ex)
            {
                Logger.Log(ex.ToString());
            }
            
        }

        /// <summary>
        /// Replaces the whole app with the contents of the zip in updater/update.zip
        /// </summary>
        /// <param name="dllName"></param>
        /// <param name="workingDir"></param>
        public static void UpdateNetApp(string dllName, string workingDir = "")
        {
            Logger.SetLogFile("updatelog.log");
            Logger.Log("Replacing everything with zip contents.");
            Thread.Sleep(1000);
            string destDir = new DirectoryInfo(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)).Parent.FullName + Path.DirectorySeparatorChar;
            using (ZipArchive archive = ZipFile.OpenRead(destDir + "updater" + Path.DirectorySeparatorChar + "update.zip"))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    String name = entry.FullName;
                    if (name.EndsWith("/")) continue;
                    if (name.Contains("/")) Directory.CreateDirectory(destDir + Path.GetDirectoryName(name));
                    Logger.Log("Extracting " + name + " to " + destDir + entry.FullName);
                    entry.ExtractToFile(destDir + entry.FullName, true);
                }
            }
            ProcessStartInfo i = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "\"" + destDir + dllName + "\" --workingdir \"" + workingDir + "\"",
                UseShellExecute = false
            };
            Process.Start(i);
            Environment.Exit(0);
        }

        public static string GetBaseDir()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static void Restart(string dllName, string workingDir = "")
        {
            ProcessStartInfo i = new ProcessStartInfo
            {
                Arguments = "\"" + AppDomain.CurrentDomain.BaseDirectory + dllName + "\" --workingdir \"" + workingDir + "\"",
                UseShellExecute = true,
                FileName = "dotnet"
            };
            Logger.Log("Starting " + i.FileName + " with args " + i.Arguments);
            Process.Start(i);
            Environment.Exit(0);
        }

        public static void StartUpdateNetApp(byte[] updateZip, string dllName, string workingDir = "")
        {
            FileManager.RecreateDirectoryIfExisting(AppDomain.CurrentDomain.BaseDirectory + "updater");
            string zip = AppDomain.CurrentDomain.BaseDirectory + "updater" + Path.DirectorySeparatorChar + "update.zip";
            string destDir = AppDomain.CurrentDomain.BaseDirectory + "updater" + Path.DirectorySeparatorChar;
            Logger.Log("Writing update zip to " + zip);
            File.WriteAllBytes(zip, updateZip);
            using (ZipArchive archive = ZipFile.OpenRead(zip))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    String name = entry.FullName;
                    if (name.EndsWith("/")) continue;
                    if (name.Contains("/")) Directory.CreateDirectory(destDir + Path.GetDirectoryName(name));
                    entry.ExtractToFile(destDir + entry.FullName, true);
                }
            }
            ProcessStartInfo i = new ProcessStartInfo
            {
                Arguments = "\"" + AppDomain.CurrentDomain.BaseDirectory + "updater" + Path.DirectorySeparatorChar + dllName + "\" update --workingdir \"" + workingDir + "\"",
                UseShellExecute = false,
                FileName = "dotnet"
            };
            Logger.Log("Starting " + i.FileName + " with args " + i.Arguments);
            Process.Start(i);
            Environment.Exit(0);
        }
    }

    public class GithubRelease
    {
        public string url { get; set; } = "";
        public string tag_name { get; set; } = "";
        public string body { get; set; } = "";
        public GithubAuthor author { get; set; } = new GithubAuthor();
        public List<GithubAsset> assets { get; set; } = new List<GithubAsset>();
        public int comparedToCurrentVersion = -2; //0 = same, -1 = earlier, 1 = newer, -2 Error

        public string GetDownload()
        {
            foreach(GithubAsset a in assets)
            {
                if (a.content_type == "application/x-zip-compressed" || a.content_type == "application/zip") return a.browser_download_url;
            }
            return "";
        }

        public Version GetVersion()
        {
            return new Version(tag_name);
        }
    }

    public class GithubAuthor
    {
        public string login { get; set; } = "";
    }

    public class GithubAsset
    {
        public string browser_download_url { get; set; } = "";
        public string content_type { get; set; } = "";
    }

    public class GithubCommit // stripped
    {
        public GithubCommitCommit commit { get; set; } = new GithubCommitCommit();
        public string html_url { get; set; } = "";
    }

    public class GithubCommitCommit // stripped
    {
        public string message { get; set; } = "";
        public GithubCommitCommiter author { get; set; } = new GithubCommitCommiter();
        public GithubCommitCommiter committer { get; set; } = new GithubCommitCommiter();

    }

    public class GithubCommitCommiter // stripped
    {
        public DateTime date { get; set; } = DateTime.MinValue;
        public string name { get; set; } = "";
        public string email { get; set; } = "";
    }
}
