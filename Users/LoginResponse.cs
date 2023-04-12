using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDB.Users
{
    public class LoginResponse
    {
        public string username { get; set; } = "";
        public string redirect { get; set; } = "";
        public string token { get; set; } = "";
        public string status { get; set; } = "This User does not exist";
        public bool authorized { get; set; } = false;
        public bool isAdmin { get; set; } = false;
    }
}
