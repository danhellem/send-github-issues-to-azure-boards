using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IssuesToWorkItems.Repo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using SyncGitHubIssuesToWorkItems.Models;
using SyncGitHubIssuesToWorkItems.Repo;
using SyncGitHubIssuesToWorkItems.ViewModels;

namespace SyncGitHubIssuesToWorkItems.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitHubIssuesController : ControllerBase
    {
        private bool _ignoreAuthCheck = false;

        private IOptions<AppSettings> _appSettings;
        private IGitHubAuthentication _gitHubAuthentication;
        private IWorkItemsRepo _workItemsRepo;

        public GitHubIssuesController(IOptions<AppSettings> appSettings, IGitHubAuthentication gitHubAuthentication, IWorkItemsRepo workItemsRepo)
        {
            _appSettings = appSettings;
            _gitHubAuthentication = gitHubAuthentication;
            _workItemsRepo = workItemsRepo;
        }

        // POST api/values
        [HttpPost]
        public ActionResult PostIssue([FromBody] JObject body)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();

            Request.Headers.TryGetValue("X-Hub-Signature", out StringValues signature);

            //check for empty signature
            if ((! _ignoreAuthCheck) && string.IsNullOrEmpty(signature))
            {
                response.Message = "Missing signature header value";
                return new StandardResponseObjectResult(response, StatusCodes.Status401Unauthorized);
            }

            //make sure something did not go wrong
            if (body == null)
            {
                response.Message = "Posted object cannot be null.";

                return new StandardResponseObjectResult(response, StatusCodes.Status400BadRequest);
            }

            string payload = JsonConvert.SerializeObject(body);

            //check body and signature to match against secret
            var isGitHubPushEventAllowed = _ignoreAuthCheck ? true : _gitHubAuthentication.IsValidGitHubWebHookRequest(payload, signature);

            //if we passed the secret check, then continue
            if (!isGitHubPushEventAllowed)
            {
                response.Message = "Invalid signature.";

                return new StandardResponseObjectResult(response, StatusCodes.Status401Unauthorized);
            }

            GitHubPostViewModel vm = this.BuildWorkingViewModel(body);
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            //look to see if work item already exist in ADO
            WorkItem workItem = _workItemsRepo.FindWorkItem(vm.number);

            //create new
            if (workItem == null && vm.action.Equals("opened"))
            {
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = vm.title + " (GitHub Issue #" + vm.number.ToString() + ")"
                    }
                );

                patchDocument.Add(
                   new JsonPatchOperation()
                   {
                       Operation = Operation.Add,
                       Path = "/fields/System.Description",
                       Value = vm.body
                   }
               );

                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = "GitHub Issue; " + vm.repo_name
                    }
                );

                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.History",
                        Value = "GitHub <a href=\"" + vm.url + "\" target=\"_new\">issue #" + vm.number + "</a> created in <a href=\"" + vm.repo_url + "\" target=\"_new\">" + vm.repo_fullname + "</a>"
                    }
                );                

                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/relations/-",
                        Value = new
                        {
                            rel = "Hyperlink",
                            url = vm.url
                        }
                    }
                );

                WorkItem createResult =_workItemsRepo.CreateWorkItem(patchDocument, vm);

                response.Message = "Successfully created work item";
                response.Success = true;
                response.Value = createResult;

                return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
            }

            //update work item (closed back to open, comment, change)
            if (workItem != null && vm.action.Equals("created") && (!String.IsNullOrEmpty(vm.comment)))
            { 
                //todo: deal with other updates like assigned to, tags, description
                
                //add comment is there is one
                if (!String.IsNullOrEmpty(vm.comment))
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.History",
                            Value = "<a href=\"" + vm.comment_url + "\" target=\"_new\">GitHub Comment Added</a></br></br>" + vm.comment
                        });
                }

                WorkItem updateResult = _workItemsRepo.UpdateWorkItem((int)workItem.Id, patchDocument, vm);

                response.Message = "Comment successfully appended to existing work item";
                response.Success = true;
                response.Value = updateResult;

                return new StandardResponseObjectResult(response, StatusCodes.Status200OK);               
            }

            //close work item
            if (workItem != null && vm.action.Equals("closed"))
            {
                patchDocument.Add(
                   new JsonPatchOperation()
                   {
                       Operation = Operation.Add,
                       Path = "/fields/System.State",
                       Value = _appSettings.Value.ADO_CloseState
                   }               
                );

                if (vm.closed_at.HasValue)
                {
                    var closedDate = vm.closed_at.Value.ToShortDateString();
                    var closedTime = vm.closed_at.Value.ToShortTimeString();

                    patchDocument.Add(
                      new JsonPatchOperation()
                      {
                          Operation = Operation.Add,
                          Path = "/fields/System.History",
                          Value = "GitHub <a href=\"" + vm.url + "\" target=\"_new\">issue #" + vm.number + "</a> was closed on " + closedDate + " at " + closedTime
                      }
                    );
                }

                //add comment is there is one
                if (!String.IsNullOrEmpty(vm.comment))
                {
                    patchDocument.Add(
                        new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/System.History",
                            Value = "<a href=\"" + vm.comment_url + "\" target=\"_new\">GitHub Comment Added</a></br></br>" + vm.comment
                        });                 
                }

                WorkItem updateResult = _workItemsRepo.UpdateWorkItem((int)workItem.Id, patchDocument, vm);

                response.Message = "Successfully closed work item";
                response.Success = true;
                response.Value = updateResult;

                return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
            }

            return null;

        }

        //// PUT api/values/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/values/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}

        private GitHubPostViewModel BuildWorkingViewModel(JObject body)
        {
            GitHubPostViewModel vm = new GitHubPostViewModel();

            vm.action = body["action"] != null ? (string)body["action"] : string.Empty;
            vm.url = body["issue"]["html_url"] != null ? (string)body["issue"]["html_url"] : string.Empty;
            vm.number = body["issue"]["number"] != null ? (int)body["issue"]["number"] : -1;
            vm.title = body["issue"]["title"] != null ? (string)body["issue"]["title"] : string.Empty;
            vm.state = body["issue"]["state"] != null ? (string)body["issue"]["state"] : string.Empty;
            vm.user = body["issue"]["user"]["login"] != null ? (string)body["issue"]["user"]["login"] : string.Empty;
            vm.body = body["issue"]["body"] != null ? (string)body["issue"]["body"] : string.Empty;
            vm.repo_fullname = body["repository"]["full_name"] != null ? (string)body["repository"]["full_name"] : string.Empty;
            vm.repo_name = body["repository"]["name"] != null ? (string)body["repository"]["name"] : string.Empty;
            vm.repo_url = body["repository"]["html_url"] != null ? (string)body["repository"]["html_url"] : string.Empty;
            vm.closed_at = body["issue"]["closed_at"] != null ? (DateTime?)body["issue"]["closed_at"] : null;

            if (body["comment"] != null)
            { 
                vm.comment = body["comment"]["body"] != null ? (string)body["comment"]["body"] : string.Empty;
                vm.comment_url = body["comment"]["html_url"] != null ? (string)body["comment"]["html_url"] : string.Empty;
            }

            if (! String.IsNullOrEmpty(vm.repo_fullname))
            {
                string[] split = vm.repo_fullname.Split('/');

                vm.organization = split[0];
                vm.repository = split[1];
            }           

            return vm;
        }
    }
}
