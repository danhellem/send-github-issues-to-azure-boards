using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebHookReciever.Models
{
    public class AppSettings
    {
        public string GitHub_Secret { get; set; }
        public string GitHub_Token { get; set; }
        public string GitHub_AppName { get; set; }
        public string ADO_Pat { get; set; }
        public string ADO_Org { get; set; }
        public string ADO_Project { get; set; }
        public string ADO_DefaultWIT { get; set; }
        public string ADO_CloseState { get; set; }
        public string  ADO_NewState { get; set; }
        public string ADO_AreaPath { get; set; }
        public string ADO_IterationPath { get; set; }
    }
}
