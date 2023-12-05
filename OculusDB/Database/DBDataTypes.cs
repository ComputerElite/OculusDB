using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBDataTypes
    {
	    public const string AppImage = "AppImage";
	    public const string ParentApplication = "ParentApplication";
	    public const string Application = "Application";
        public const string Version = "Version";
        public const string IAPItem = "IAPItem";
        public const string IAPItemPack = "IAPItemPack";
        public const string OBBBinary = "OBBBinary";
		public const string VersionAlias = "VersionAlias";
		public const string Price = "Price";
		public const string ReleaseChannel = "ReleaseChannel";
		public const string ApplicationTranslation = "ApplicationTranslation";
		public const string ApplicationGrouping = "ApplicationGrouping";
		public const string ParentApplicationGrouping = "ParentApplicationGrouping";
		public const string AssetFile = "AssetFile";
		public const string IAPItemId = "IAPItemId";
		public const string Achievement = "Achievement";
		public const string AchievementTranslation = "AchievementTranslation";
		public const string Offer = "Offer";
		public const string Error = "Error";
		public const string Unknown = "Unknown";
		
        // not a DB property. Gets resolved on runtime for webhooks
        public const string ActivityVersionDownloadable = "ActivityVersionDownloadable";
    }
}
