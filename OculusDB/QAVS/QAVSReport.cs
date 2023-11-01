using ComputerUtils.VarUtils;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.QAVS
{
	[BsonIgnoreExtraElements]
	public class QAVSReport
	{
		public string log { get; set; }
		public string modloaderMode { get; set; } = "QuestLoader";
		public int androidVersion { get; set; } = 29; // Android 10
		public string version { get; set; }
		public DateTime reportTime { get; set; }
		public string reportId { get; set; }
		public string device { get; set; }
		public bool userIsLoggedIn { get; set; }
		public List<string> userEntitlements { get; set; } = new List<string>();
		public long availableSpace { get; set; }
        public PatchingStatus appStatus { get; set; }
		public string availableSpaceString { get
			{
				return SizeConverter.ByteSizeToString(availableSpace);
			} }

		public ModsAndLibs modsAndLibs { get; set; } = null;
	}
	public class PatchingStatus
	{
		public bool isPatched { get; set; } = false;
		public bool isInstalled { get; set; } = true;
		public bool canBePatched { get; set; } = true; // Not implemented yet.
		public string version { get; set; } = "";
		public string versionCode { get; set; } = "";
		public ModdedJson moddedJson { get; set; } = null;
	}
	public class ModdedJson
	{
		public string patcherName { get; set; } = "";
		public string patcherVersion { get; set; } = "0.0.0";
		public string modloaderName { get; set; } = "";
		public string modloaderVersion { get; set; } = "";
		public List<string> modifiedFiles { get; set; } = new List<string>();
	}

	public class ModsAndLibs
	{
		public List<IMod> mods { get; set; } = new List<IMod>();
		public List<IMod> libs { get; set; } = new List<IMod>();
	}

	public class IModProvider
	{
		public string FileExtension { get; set; } = "";
	}

	public class IMod
	{
		/// <summary>
        /// Provider that loaded this mod
        /// </summary>
		public IModProvider Provider { get; set; }

        /// <summary>
        /// Unique ID of the mod, must not contain spaces
        /// </summary>
        public string Id { get; set; }
        
        public bool hasCover { get; set; }

        /// <summary>
        /// Human readable name of the mod
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the mod
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Version of the mod
        /// </summary>
        public string VersionString { get; set; }

        /// <summary>
        /// Version of the package that the mod is intended for
        /// </summary>
        public string? PackageVersion { get; set; }

        /// <summary>
        /// Author of the mod
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Individual who ported this mod from another platform
        /// </summary>
        public string? Porter { get; set; }

        /// <summary>
        /// Whether or not the mod is currently installed
        /// </summary>
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Whether or not the mod is a library
        /// </summary>
        public bool IsLibrary { get; set; }
	}
}
