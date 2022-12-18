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
		public string version { get; set; }
		public DateTime reportTime { get; set; }
		public string reportId { get; set; }
		public bool userIsLoggedIn { get; set; }
		public List<string> userEntitlements { get; set; }
	}
}
