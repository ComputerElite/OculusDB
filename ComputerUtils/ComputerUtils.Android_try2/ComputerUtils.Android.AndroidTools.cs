using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using ComputerUtils.Android.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Essentials;
using static Xamarin.Essentials.Permissions;
using Uri = Android.Net.Uri;

namespace ComputerUtils.Android.AndroidTools
{
    public class AssetTools
    {
        public static byte[] GetAssetBytes(string assetName)
        {
            MemoryStream ms = new MemoryStream();
            AndroidCore.assetManager.Open(assetName).CopyTo(ms);
            return ms.ToArray();
        }

        public static string GetAssetString(string assetName)
        {
            return new StreamReader(AndroidCore.assetManager.Open(assetName)).ReadToEnd();
        }

        public static bool DoesAssetExist(string assetName)
        {
            //GetAllFiles("").ForEach(e => Logger.Log("\"" + e + "\" == \"" + assetName + "\" = " + (e == assetName).ToString(), LoggingType.Debug));
            return GetAllFiles("").Contains(assetName);
        }

        public static List<string> GetAllFiles(string folder)
        {
            List<string> files = new List<string>();
            if (!folder.EndsWith("/")) folder += "/";
            if (folder == "/") folder = "";
            foreach (string s in AndroidCore.assetManager.List(folder))
            {
                files.Add(folder + s);
                foreach (string ss in GetAllFiles(folder + s)) files.Add(ss);
            }
            return files;
        }

        public static List<string> GetAssetFolderFileList(string assetFolder)
        {
            return new List<string>(AndroidCore.assetManager.List(assetFolder));
        }
    }

    public class AndroidService
    {
        public static List<App> GetInstalledApps()
        {
            List<App> inApps = new List<App>();
            IList<ApplicationInfo> apps = Application.Context.PackageManager.GetInstalledApplications(PackageInfoFlags.MatchAll);
            for (int i = 0; i < apps.Count; i++)
            {
                inApps.Add(new App(apps[i].LoadLabel(Application.Context.PackageManager), apps[i].PackageName));
            }
            return inApps;
        }

        public static string FindAPKLocation(string package)
        {
            try
            {
                ApplicationInfo applicationInfo = Application.Context.PackageManager.GetApplicationInfo(package, PackageInfoFlags.MatchAll);
                return (applicationInfo != null) ? applicationInfo.SourceDir : null;
            }
            catch (PackageManager.NameNotFoundException)
            {
            }
            return null;
        }

        public static void InitiateUninstallPackage(string package)
        {
            Intent uninstallIntent = new Intent(Intent.ActionDelete, Uri.Parse("package:" + package));
            //uninstallIntent.AddFlags(ActivityFlags.NewTask);
            AndroidCore.context.StartActivity(uninstallIntent);
        }

        public static bool IsPackageInstalled(string package)
        {
            bool installed = false;
            foreach (App a in GetInstalledApps())
            {
                if (a.PackageName == package) { installed = true; break; }
            }
            return installed;
        }

        public static void InitiateInstallApk(string apkLocation)
        {
            Intent intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(Uri.FromFile(new Java.IO.File(apkLocation)), "application/vnd.android.package-archive");
            //intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
            intent.SetFlags(ActivityFlags.GrantReadUriPermission);
            AndroidCore.context.StartActivity(intent);
        }
    }

    public class App
    {
        public string AppName { get; set; }
        public string PackageName { get; set; }

        public App(string appName, string packageName)
        {
            AppName = appName;
            PackageName = packageName;
        }
    }
}
