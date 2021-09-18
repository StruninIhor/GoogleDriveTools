using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleDriveTools
{
    public class GoogleDriveConfig
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string ApplicationName { get; set; }

        public string Username { get; set; }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }
}
