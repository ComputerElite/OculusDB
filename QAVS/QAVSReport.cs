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
		public int androidVersion { get; set; } = 29; // Android 10
		public string version { get; set; }
		public DateTime reportTime { get; set; }
		public string reportId { get; set; }
		public bool userIsLoggedIn { get; set; }
		public List<string> userEntitlements { get; set; } = new List<string>();
		public long availableSpace { get; set; }
		public string availableSpaceString { get
			{
				return SizeConverter.ByteSizeToString(availableSpace);
			} }
	}
}
