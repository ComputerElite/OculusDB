﻿using ComputerUtils.FileManaging;
using ComputerUtils.RandomExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerUtils.CommandLine;
using ComputerUtils.Updating;
using OculusDB.ScrapingNodeCode;

namespace OculusDB
{
    public class OculusDBEnvironment
    {
        public static Updater updater = new ("1.1.33", "https://github.com/ComputerElite/OculusDB", "OculusDB", "OculusDB.dll");
        public static string workingDir = "";
        public static string dataDir = "";
        // Set to false if not in dev mode
        public static bool debugging = false;
        public static Config config = new ();
        public static ScrapingNodeConfig scrapingNodeConfig = new ();
        public static CommandLineCommandContainer cla;

        public static string userAgent { get
            {
                return "OculusDB/" + updater.version;
            } }

        public static void AddVariablesDependentOnVariablesAndFixAllOtherVariables()
        {
            if (!workingDir.EndsWith(Path.DirectorySeparatorChar)) workingDir += Path.DirectorySeparatorChar;
            if (workingDir == Path.DirectorySeparatorChar.ToString()) workingDir = AppDomain.CurrentDomain.BaseDirectory;
            dataDir = workingDir + "data" + Path.DirectorySeparatorChar;
            FileManager.CreateDirectoryIfNotExisting(workingDir);
            FileManager.CreateDirectoryIfNotExisting(dataDir);
        }
    }
}
