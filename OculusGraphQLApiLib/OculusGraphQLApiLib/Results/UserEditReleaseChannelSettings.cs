using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusGraphQLApiLib.Results
{
	public class UserEditReleaseChannelSettings
	{
		public ReleaseChannelSettingWrapper data { get; set; } = new ReleaseChannelSettingWrapper();
	}

	public class ReleaseChannelSettingWrapper
	{
		public ApplicationWrapper application { get; set; } = new ApplicationWrapper();
	}

	public class ApplicationWrapper
	{
		public Application user_edit_release_channel_settings { get; set; } = new Application();
	}

}
