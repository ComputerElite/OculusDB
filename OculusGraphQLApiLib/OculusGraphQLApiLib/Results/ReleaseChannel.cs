namespace OculusGraphQLApiLib.Results
{
    public class ReleaseChannel : ReleaseChannelWithoutLatestSupportedBinary
    {
        public AndroidBinary latest_supported_binary { get; set; } = new AndroidBinary();
        
    }
    
    public class ReleaseChannelWithoutLatestSupportedBinary
    {
        public string id { get; set; } = "";
        public string channel_name { get; set; } = "";
        
    }
}