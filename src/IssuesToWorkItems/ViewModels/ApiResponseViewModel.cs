using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncGitHubIssuesToWorkItems.ViewModels
{
    public class ApiResponseViewModel : IApiResponseViewModel
    {
        public ApiResponseViewModel()
        {
            Success = false;
            Count = 0;
            Value = null;
            Message = String.Empty;
        }

        [JsonProperty(PropertyName = "success")]
        public bool Success { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "value")]
        public object Value { get; set; }

        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
    }

    public interface IApiResponseViewModel
    {
        bool Success { get; set; }
        string Message { get; set; }
        object Value { get; set; }
        int Count { get; set; }
    }
}
