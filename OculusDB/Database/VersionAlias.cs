using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson;
using OculusGraphQLApiLib.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MongoDB.Driver;
using OculusDB.MongoDB;
using OculusGraphQLApiLib;

namespace OculusDB.Database
{
	[BsonIgnoreExtraElements]
	public class VersionAlias : DBBase
	{
		public override string __OculusDBType { get; set; } = DBDataTypes.VersionAlias;
		public string alias { get; set; } = "";
		public string appId { get; set; } = "";
		public string appName { get; set; } = "";
		public string versionId { get; set; } = "";
		
		public static List<VersionAlias> GetVersionAliases(string appId)
		{
			return OculusDBDatabase.versionAliases.Find(x => x.appId == appId).ToList();
		}

		public static List<VersionAlias> GetApplicationsWithAliases()
		{
			List<string> apps = OculusDBDatabase.versionAliases.Distinct(x => x.appId, new BsonDocument()).ToList();
			List<VersionAlias> aliases = new List<VersionAlias>();
			for (int i = 0; i < apps.Count; i++)
			{
				DBApplication? application = DBApplication.ById(apps[i]);
				if (application == null) continue;
				aliases.Add(new VersionAlias { appId = apps[i], appName = application.displayName });
			}
			return aliases;
		}

		public static VersionAlias GetVersionAlias(string versionId)
		{
			return OculusDBDatabase.versionAliases.Find(x => x.versionId == versionId).FirstOrDefault();
		}

		public static void AddVersionAlias(VersionAlias alias)
		{
			OculusDBDatabase.versionAliases.DeleteMany(x => x.versionId == alias.versionId);
			OculusDBDatabase.versionAliases.InsertOne(alias);
		}

		public static void RemoveVersionAlias(VersionAlias alias)
		{
			OculusDBDatabase.versionAliases.DeleteMany(x => x.versionId == alias.versionId);
		}
	}
}
