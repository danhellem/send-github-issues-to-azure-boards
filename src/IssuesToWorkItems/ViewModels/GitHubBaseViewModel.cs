using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebHookReciever.ViewModels
{
    public class GitHubBaseViewModel
    {
        public string organization { get; set; }       
        public string repository { get; set; }
    }
}
