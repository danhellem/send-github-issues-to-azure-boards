using System;

using WebHookReciever.Repo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using WebHookReciever.Models;
using WebHookReciever.ViewModels;

namespace WebHookReciever.Controllers
{
    [Route("api/issues")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
        private bool _ignoreAuthCheck = false;

        private IOptions<AppSettings> _appSettings;
        private IGitHubAuthentication _gitHubAuthentication;
        private IWorkItemsRepo _workItemsRepo;

        public IssuesController(IOptions<AppSettings> appSettings, IGitHubAuthentication gitHubAuthentication, IWorkItemsRepo workItemsRepo)
        {
            _appSettings = appSettings;
            _gitHubAuthentication = gitHubAuthentication;
            _workItemsRepo = workItemsRepo;

#if DEBUG 
            _ignoreAuthCheck = true;
#endif
        }

        // POST api/values
        [HttpPost]
        public ActionResult Post([FromBody] JObject body)
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

            //look to see if work item already exist in ADO
            WorkItem workItem = _workItemsRepo.FindWorkItem(vm.number, vm.repo_name);

            // if the work item is empty that means it did not exist before the webhook was created
            // so lets go create the work item in ADO and link it
            if (workItem == null)
            {
                workItem = this.CreateNewWorkItem(vm);                
            }

            switch (vm.action)
            {
                case "opened":
                    return workItem == null ? this.CreateNew(vm) : new StandardResponseObjectResult("existing work item found", StatusCodes.Status201Created);
                case "edited":
                    return workItem != null ? this.UpdateEdited(vm, workItem) : new StandardResponseObjectResult("work item not found", StatusCodes.Status200OK);
                case "created": //comment                      
                    return workItem != null ? this.AppendComment(vm, (int)workItem.Id) : new StandardResponseObjectResult("work item not found", StatusCodes.Status200OK);
                case "reopened":
                    return workItem != null ? this.ReOpen(vm, (int)workItem.Id) : new StandardResponseObjectResult("work item not found", StatusCodes.Status200OK);
                case "closed":
                    return workItem != null ? this.Close(vm, (int)workItem.Id) : new StandardResponseObjectResult("work item not found", StatusCodes.Status200OK);
                case "assigned":                   
                    return new StandardResponseObjectResult("assigned action not implemented", StatusCodes.Status200OK);
                case "labeled":
                    return workItem != null ? this.AddLabel(vm, workItem) : new StandardResponseObjectResult("work item not found", StatusCodes.Status200OK);
                case "deleted":
                    return new StandardResponseObjectResult("delete action not implemented", StatusCodes.Status200OK);
                default:
                    return new StandardResponseObjectResult("action not found", StatusCodes.Status200OK);                    
            }
        }        

        private StandardResponseObjectResult UpdateEdited(GitHubPostViewModel vm, WorkItem workItem)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            //if title changes
            if (! workItem.Fields["System.Title"].Equals(vm.title + " (GitHub Issue #" + vm.number.ToString() + ")"))
            {
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Title",
                        Value = vm.title + " (GitHub Issue #" + vm.number.ToString() + ")"
                    });
            }


            // if description changed
            if (! workItem.Fields["System.Description"].Equals(vm.body))            { 
               
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Description",
                        Value = vm.body
                    }
                );
            }

            // if nothing in the patch document, then don't update
            if (patchDocument.Count > 0)
            { 
                WorkItem updateResult = _workItemsRepo.UpdateWorkItem((int)workItem.Id, patchDocument, vm);

                response.Message = "Work item successfully updated";
                response.Success = true;
                response.Value = updateResult;

                patchDocument = null;
            }
            else
            {
                response.Message = "No changes to be made";
                response.Success = true;
                response.Value = null;
            }

            return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
                     
        }

        private StandardResponseObjectResult ReOpen(GitHubPostViewModel vm, int workItemId)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
              new JsonPatchOperation()
              {
                  Operation = Operation.Add,
                  Path = "/fields/System.State",
                  Value = _appSettings.Value.ADO_NewState
              }
           );

            patchDocument.Add(
               new JsonPatchOperation()
               {
                   Operation = Operation.Add,
                   Path = "/fields/System.History",
                   Value = "Issue reopened"
               });

            WorkItem updateResult = _workItemsRepo.UpdateWorkItem(workItemId, patchDocument, vm);

            response.Message = "Issue successfully reopened";
            response.Success = true;
            response.Value = null;

            patchDocument = null;

            return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
           
        }

        private StandardResponseObjectResult CreateNew(GitHubPostViewModel vm)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();

            WorkItem createResult = this.CreateNewWorkItem(vm);

            response.Message = "Successfully created work item";
            response.Success = true;
            response.Value = createResult;           

            return new StandardResponseObjectResult(response, StatusCodes.Status200OK);            
        }

        private WorkItem CreateNewWorkItem(GitHubPostViewModel vm)        {
            
            JsonPatchDocument patchDocument = new JsonPatchDocument();

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

            WorkItem createResult = _workItemsRepo.CreateWorkItem(patchDocument, vm);
           
            patchDocument = null;

            return createResult;
        }
        
        private StandardResponseObjectResult AppendComment(GitHubPostViewModel vm, int workItemId)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();
            JsonPatchDocument patchDocument = new JsonPatchDocument();
           
            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.History",
                    Value = "<a href=\"" + vm.comment_url + "\" target=\"_new\">GitHub Comment Added</a></br></br>" + vm.comment
                });
            

            WorkItem updateResult = _workItemsRepo.UpdateWorkItem(workItemId, patchDocument, vm);

            response.Message = "Comment successfully appended to existing work item";
            response.Success = true;
            response.Value = updateResult;

            patchDocument = null;

            return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
        }

        private StandardResponseObjectResult Close(GitHubPostViewModel vm, int workItemId)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();
            JsonPatchDocument patchDocument = new JsonPatchDocument();

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

            WorkItem updateResult = _workItemsRepo.UpdateWorkItem(workItemId, patchDocument, vm);

            response.Message = "Successfully closed work item";
            response.Success = true;
            response.Value = updateResult;

            return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
        }

        private StandardResponseObjectResult AddLabel(GitHubPostViewModel vm, WorkItem workItem)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            if (! workItem.Fields["System.Tags"].ToString().Contains(vm.label))
            { 
                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/System.Tags",
                        Value = workItem.Fields["System.Tags"].ToString() + ", " + vm.label
                    }
                );

                WorkItem updateResult = _workItemsRepo.UpdateWorkItem(Convert.ToInt32(workItem.Id), patchDocument, vm);

                response.Message = "Label successfully update on work item";
                response.Success = true;
                response.Value = updateResult;
            }
            else
            {
                response.Message = "Tag already exists on the work item";
                response.Success = true;
                response.Value = null;
            }

            patchDocument = null;

            return new StandardResponseObjectResult(response, StatusCodes.Status200OK);
        }

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

            if (body["label"] != null)
            {
                vm.label = body["label"]["name"] != null ? (string)body["label"]["name"] : string.Empty;
            }            

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
