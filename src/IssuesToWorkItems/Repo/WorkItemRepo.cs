using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SyncGitHubIssuesToWorkItems.Models;
using SyncGitHubIssuesToWorkItems.ViewModels;

namespace IssuesToWorkItems.Repo
{
    public class WorkItemRepo
    {
        private IOptions<AppSettings> _appSettings;

        public WorkItemRepo(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        public WorkItem CreateWorkItem(JsonPatchDocument patchDocument, GitHubPostViewModel vm)
        {
            string pat = vm.pat;
            Uri baseUri = new Uri("https://dev.azure.com/" + vm.organization);

            VssCredentials clientCredentials = new VssCredentials(new VssBasicCredential("username", pat));
            VssConnection connection = new VssConnection(baseUri, clientCredentials);

            WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItem result = null;

            Wiql wiql = new Wiql()
            {
                Query = "SELELCT [System.Id] FROM workitems [System.TeamProject] = @project AND [System.Title] CONTAINS WORDS '(GitHub Issue #114)' AND [System.Tags] CONTAINS 'GitHub Issue'"
            };

            try
            {
                var queryResults = client.QueryByWiqlAsync(wiql, vm.project).Result;
                
                result = (queryResults == null) ? client.CreateWorkItemAsync(patchDocument, vm.project, vm.type).Result : null;
                              
            }
            catch (Exception ex)
            {
                result = null;
            }
            finally
            {
                clientCredentials = null;
                connection = null;
                client = null;
            }

            return result;
        }
    }
}
