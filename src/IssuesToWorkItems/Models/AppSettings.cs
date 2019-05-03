using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IssuesToWorkItems.Models
{
    public class AppSettings
    {
        public string GitHubSecret { get; set; }
        public string GitHubToken { get; set; }
        public string GitHubAppName { get; set; }
    }
}
