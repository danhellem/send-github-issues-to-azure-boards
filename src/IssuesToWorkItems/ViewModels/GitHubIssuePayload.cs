using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IssuesToWorkItems.ViewModels
{
    public class GitHubIssuePayload : BaseViewModel
    {
        public int id { get; set; }
        public string type {  get; set; }
    }
}
