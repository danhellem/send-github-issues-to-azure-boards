using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SyncGitHubIssuesToWorkItems.Models;
using SyncGitHubIssuesToWorkItems.Repo;
using SyncGitHubIssuesToWorkItems.ViewModels;

namespace SyncGitHubIssuesToWorkItems.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitHubIssuesController : ControllerBase
    {
        private IOptions<AppSettings> _appSettings;
        private IGitHubAuthentication _gitHubAuthentication;

        public GitHubIssuesController(IOptions<AppSettings> appSettings, IGitHubAuthentication gitHubAuthentication)
        {
            _appSettings = appSettings;
            _gitHubAuthentication = gitHubAuthentication;
        }

        // POST api/values
        [HttpPost]
        public async Task<ActionResult> PostIssue([FromBody] JObject body)
        {
            ApiResponseViewModel response = new ApiResponseViewModel();

            Request.Headers.TryGetValue("X-Hub-Signature", out StringValues signature);

            //check for empty signature
            if (string.IsNullOrEmpty(signature))
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
            var isGitHubPushEventAllowed = _gitHubAuthentication.IsValidGitHubWebHookRequest(payload, signature);

            //if we passed the secret check, then continue
            if (!isGitHubPushEventAllowed)
            {
                response.Message = "Invalid signature.";

                return new StandardResponseObjectResult(response, StatusCodes.Status401Unauthorized);
            }

           GitHubPostViewModel vm = this.BuildWorkingViewModel(body);

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        private GitHubPostViewModel BuildWorkingViewModel(JObject body)
        {
            GitHubPostViewModel vm = new GitHubPostViewModel();

            vm.action = (string)body["action"];
            vm.url = (string)body["url"];
            vm.number = (int)body["number"];
            vm.title = (string)body["title"];
            vm.state = (string)body["state"];
            vm.user = (string)body["user"]["login"];
            vm.body = (string)body["body"];
            vm.repo_fullname = (string)body["repository"]["full_name"];     
            vm.repo_url = (string)body["repository"]["html_url"];

            string[] split = vm.repo_fullname.Split('/');

            vm.organization = split[0];
            vm.repository = split[1];

            return vm;
        }
    }
}
