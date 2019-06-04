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

using WebHookReciever.Models;
using WebHookReciever.ViewModels;

namespace WebHookReciever.Repo
{
    public class WorkItemsRepo : IWorkItemsRepo
    {
        private IOptions<AppSettings> _options;       

        public WorkItemsRepo(IOptions<AppSettings> options)
        {
            _options = options;            
        }

        public WorkItem FindWorkItem(int number, string repo)
        {
            string pat = _options.Value.ADO_Pat;
            string org = _options.Value.ADO_Org;
            string project = _options.Value.ADO_Project;               

            Uri baseUri = new Uri("https://dev.azure.com/" + org);

            VssCredentials clientCredentials = new VssCredentials(new VssBasicCredential("username", pat));
            VssConnection connection = new VssConnection(baseUri, clientCredentials);

            WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItem result = null;

            Wiql wiql = new Wiql()
            {
                Query = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State] FROM workitems WHERE [System.TeamProject] = @project AND [System.Title] CONTAINS '(GitHub Issue #" + number + ")' AND [System.Tags] CONTAINS 'GitHub Issue' AND [System.Tags] CONTAINS '" + repo + "'"
            };

            try
            {
                WorkItemQueryResult queryResult = client.QueryByWiqlAsync(wiql, project).Result;
                WorkItemReference workItem = null;

                workItem = queryResult.WorkItems.Count() > 0 ? queryResult.WorkItems.First() : null;

                result = workItem != null ? client.GetWorkItemAsync(workItem.Id, null, null, WorkItemExpand.All).Result : null;
            }
            catch (Exception)
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

        public WorkItem CreateWorkItem(JsonPatchDocument patchDocument, GitHubPostViewModel vm)
        {
            string pat = _options.Value.ADO_Pat;
            string org = _options.Value.ADO_Org;
            string project = _options.Value.ADO_Project;
            string wit = _options.Value.ADO_DefaultWIT;
          
            Uri baseUri = new Uri("https://dev.azure.com/" + org);

            VssCredentials clientCredentials = new VssCredentials(new VssBasicCredential("username", pat));
            VssConnection connection = new VssConnection(baseUri, clientCredentials);

            WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItem result = null;            

            try
            {
                result = client.CreateWorkItemAsync(patchDocument, project, wit).Result;
            }
            catch (Exception)
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

        public WorkItem UpdateWorkItem(int id, JsonPatchDocument patchDocument, GitHubPostViewModel vm)
        {
            string pat = _options.Value.ADO_Pat;
            string org = _options.Value.ADO_Org;           

            Uri baseUri = new Uri("https://dev.azure.com/" + org);

            VssCredentials clientCredentials = new VssCredentials(new VssBasicCredential("username", pat));
            VssConnection connection = new VssConnection(baseUri, clientCredentials);

            WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItem result = null;

            try
            {
                result = client.UpdateWorkItemAsync(patchDocument, id).Result;
            }
            catch (Exception)
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

        public WorkItemQueryResult QueryWorkItems()
        {
            string pat = _options.Value.ADO_Pat;
            string org = _options.Value.ADO_Org;
            string project = _options.Value.ADO_Project;         

            Uri baseUri = new Uri("https://dev.azure.com/" + org);

            VssCredentials clientCredentials = new VssCredentials(new VssBasicCredential("username", pat));
            VssConnection connection = new VssConnection(baseUri, clientCredentials);

            WorkItemTrackingHttpClient client = connection.GetClient<WorkItemTrackingHttpClient>();
            WorkItemQueryResult result = null;

            Wiql wiql = new Wiql()
            {
                Query = "SELECT [System.Id], [System.Title], [System.State] FROM workitems WHERE [System.TeamProject] = @project AND [System.WorkItemType] = 'Issue' AND [System.State] <> 'Done' AND [System.Tags] CONTAINS 'GitHub Issue' AND [System.BoardColumn] = 'To Do'"
            };
               
            try
            {
                result = client.QueryByWiqlAsync(wiql, project).Result;
            }
            catch (Exception)
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

    public interface IWorkItemsRepo
    {
        WorkItem FindWorkItem(int number, string repo);
        WorkItem CreateWorkItem(JsonPatchDocument patchDocument, GitHubPostViewModel vm);
        WorkItem UpdateWorkItem(int id, JsonPatchDocument patchDocument, GitHubPostViewModel vm);
        WorkItemQueryResult QueryWorkItems();
    }
}
