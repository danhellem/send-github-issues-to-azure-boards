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
using System.Collections.Generic;
using System.Linq;

namespace WebHookReciever.Controllers
{
    [Route("api/workitems")]
    [ApiController]
    public class WorkItemsController : ControllerBase
    {        
        private IOptions<AppSettings> _appSettings;
      
        private IWorkItemsRepo _workItemsRepo;

        public WorkItemsController(IOptions<AppSettings> appSettings, IWorkItemsRepo workItemsRepo)
        {
            _appSettings = appSettings;           
            _workItemsRepo = workItemsRepo;
        }

        // POST api/values
        [HttpGet]
        [Route("new/count")]
        public ActionResult GetCount()
        {
            ApiResponseViewModel response = new ApiResponseViewModel();

            WorkItemQueryResult results = _workItemsRepo.QueryWorkItems(); 
            int count = results != null ? results.WorkItems.Count() : 0;

            return new StandardResponseObjectResult(count, StatusCodes.Status200OK);      
        }        
    }
}
