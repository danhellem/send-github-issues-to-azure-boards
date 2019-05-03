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

using IssuesToWorkItems.Models;
using IssuesToWorkItems.ViewModels;

namespace IssuesToWorkItems.Repo
{
    public class WorkItemRepo
    {
        private IOptions<AppSettings> _appSettings;

        public WorkItemRepo(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        public WorkItem CreateWorkItem(JsonPatchDocument patchDocument, GitHubIssuePayload vm)
        {
            string pat = vm.pat;
            Uri baseUri = new Uri("https://dev.azure.com/" + vm.organization);

            VssCredentials clientCredentials = new VssCredentials(new VssBasicCredential("username", pat));
            VssConnection connection = new VssConnection(baseUri, clientCredentials);

            WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItem result = null;

            Wiql wiql = new Wiql()
            {
                Query = "SELELCT [System.Id] FROM workitems WHERE [System.Tags] CONTAINS 'GitHub Issues #" + vm.id.ToString() + "'"
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
