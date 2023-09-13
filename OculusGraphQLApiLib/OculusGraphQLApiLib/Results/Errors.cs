using System.Collections.Generic;

namespace OculusGraphQLApiLib.Results
{
    public class Error
    {
        public string message { get; set; } = "";
        public string serverity { get; set; } = "";
        public string type { get; set; } = "";
        public int code { get; set; } = -1;
        public string fbtrace_id { get; set; } = "";
        public List<object> path { get; set; } = new List<object>();
    }

    public class ErrorContainer
    {
        public Error error { get; set; } = new Error();
    }
}