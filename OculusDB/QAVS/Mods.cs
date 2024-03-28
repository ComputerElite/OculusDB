using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.QAVS
{
	public class QMod
	{
		public string _QPVersion { get; set; } = "0.1.1";
		public string name { get; set; } = "mod";
		public string id { get; set; } = "mod_id";
		public string author { get; set; } = "OculusDB";
		public string description { get; set; } = "Downloads all Core mods for Beat Saber version 1.27.0";
		public string version { get; set; } = "1.0.0";
		public string packageId { get; set; } = "com.beatgames.beatsaber";
		public string packageVersion { get; set; } = "1.27.0";
		public string modloader { get; set; } = "QuestLoader";
		public object[] modFiles { get; set; } = new object[0];
		public object[] libraryFiles { get; set; } = new object[0];
		public object[] fileCopies { get; set; } = new object[0];
		public object[] copyExtensions { get; set; } = new object[0];
		public List<QModDependency> dependencies { get; set; } = new List<QModDependency>();
	}

	public class QModDependency
	{
		public string id { get; set; } = "mod_id";
		public string version { get; set; } = "1.0.0";
		public string downloadIfMissing { get; set; } = "1.0.0";
	}

	public class CoreMods
	{
		public string lastUpdated { get; set; } = "";
		public List<CoreMod> mods { get; set; } = new List<CoreMod>();
	}

	public class CoreMod
	{
		public string id { get; set; } = "";
		public string version { get; set; } = "";
		public string downloadLink { get; set; } = "";
	}
}
