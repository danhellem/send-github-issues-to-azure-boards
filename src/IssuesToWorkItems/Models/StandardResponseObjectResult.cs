using Microsoft.AspNetCore.Mvc;

namespace IssuesToWorkItems.Models
{
    public class StandardResponseObjectResult : ObjectResult
    {
        public StandardResponseObjectResult(object value, int statusCode) : base(value)
        {
            StatusCode = statusCode;
        }
    }
}
