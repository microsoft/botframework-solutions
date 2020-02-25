using Newtonsoft.Json;
using System.Collections.Generic;

namespace EmailSkill.Models.Action
{
    public class SummaryResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }

        [JsonProperty("emailList")]
        public List<EmailInfo> EmailList { get; set; }
    }
}
