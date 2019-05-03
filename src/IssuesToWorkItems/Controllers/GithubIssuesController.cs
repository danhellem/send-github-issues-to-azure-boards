using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;

using IssuesToWorkItems.Models;

namespace IssuesToWorkItems.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GitHubIssuesController : ControllerBase
    {
        IOptions<AppSettings> _appSettings;

        public GitHubIssuesController(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        // POST api/values
        [HttpPost]
        public async Task<ActionResult> PostIssue([FromBody] JObject payload)
        {
            string tags = Request.Headers.ContainsKey("Work-Item-Tags") ? Request.Headers["Work-Item-Tags"] : new StringValues("");
            string authHeader = Request.Headers.ContainsKey("Authorization") ? Request.Headers["Authorization"] : new StringValues("");

            if (!authHeader.StartsWith("Basic"))
            {
                return new StandardResponseObjectResult("missing basic authorization header", StatusCodes.Status401Unauthorized);
            }

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
    }
}
