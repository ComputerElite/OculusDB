using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Database
{
    public class DBDataTypes
    {
        public const string Application = "Application";
        public const string Version = "Version";
        public const string IAPItem = "IAPItem";
        public const string IAPItemPack = "IAPItemPack";
        public const string OBBBinary = "OBBBinary";


        //Activities
        public const string ActivityNewApplication = "ActivityNewApplication";
        public const string ActivityPriceChanged = "ActivityPriceChanged";
        public const string ActivityNewVersion = "ActivityNewVersion";
        public const string ActivityVersionUpdated = "ActivityVersionUpdated";
		public const string ActivityVersionChangelogAvailable = "ActivityVersionChangelogAvailable";
		public const string ActivityVersionChangelogUpdated = "ActivityVersionChangelogUpdated";

		public const string ActivityNewDLC = "ActivityNewDLC";
        public const string ActivityDLCUpdated = "ActivityDLCUpdated";
        public const string ActivityNewDLCPack = "ActivityNewDLCPack";
        public const string ActivityDLCPackUpdated = "ActivityDLCPackUpdated";

        // not a DB property. Gets resolved on runtime for webhooks
        public const string ActivityVersionDownloadable = "ActivityVersionDownloadable";
    }
}
