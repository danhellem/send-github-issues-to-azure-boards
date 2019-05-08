using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncGitHubIssuesToWorkItems.ViewModels
{
    public class GitHubPostViewModel : GitHubBaseViewModel
    {
        public string action { get; set; }
        public string url { get; set; }
        public int number { get; set; }
        public string title { get; set; }
        public string state { get; set; }
        public string user { get; set; }
        public string body { get; set; }
        public string repo_fullname { get; set; }
        public string repo_url { get; set; }
    }
}
