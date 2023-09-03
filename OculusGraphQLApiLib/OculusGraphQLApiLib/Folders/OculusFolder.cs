using OculusGraphQLApiLib.Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Folders
{
    public class OculusFolder
    {
        public static string GetSoftwareDirectory(string oculusFolder, string canonicalName)
        {
            return oculusFolder + Path.DirectorySeparatorChar + "Software" + Path.DirectorySeparatorChar + canonicalName + Path.DirectorySeparatorChar;
        }

        public static string GetManifestPath(string oculusFolder, string canonicalName)
        {
            return oculusFolder + Path.DirectorySeparatorChar + "Manifests" + Path.DirectorySeparatorChar + canonicalName + ".json";
        }

        public static Manifest GetManifest(string oculusFolder, string canonicalName)
        {
            return JsonSerializer.Deserialize<Manifest>(File.ReadAllText(GetManifestPath(oculusFolder, canonicalName)));
        }

        public static List<string> GetCanonicalNamesOfInstalledApps(string oculusFolder)
        {
            return Directory.GetDirectories(oculusFolder + Path.DirectorySeparatorChar + "Software").ToList().ConvertAll(x => Path.GetFileName(x));
        }
    }
}
