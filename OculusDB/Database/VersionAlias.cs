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
		public Headset appHeadset { get; set; } = Headset.INVALID;
		public string versionId { get; set; } = "";
	}
}
