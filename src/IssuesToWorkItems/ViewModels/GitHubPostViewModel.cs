using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncGitHubIssuesToWorkItems.ViewModels
{
    public class GitHubPostViewModel : GitHubBaseViewModel
    {
        public string action { get; set; } = string.Empty;
        public string url { get; set; } = string.Empty;
        public int number { get; set; } = -1;
        public string title { get; set; } = string.Empty;
        public string state { get; set; } = string.Empty;
        public string user { get; set; } = string.Empty;
        public string body { get; set; } = string.Empty;
        public string repo_fullname { get; set; } = string.Empty;
        public string repo_url { get; set; } = string.Empty;
        public string comment { get; set; } = string.Empty;
        public string comment_url { get; set; } = string.Empty;

        public DateTime? closed_at { get; set; } = null;
    }
}
