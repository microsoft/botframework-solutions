using Newtonsoft.Json;
using System.Collections.Generic;

namespace EmailSkill.Models.Action
{
    public class ForwardEmailInfo
    {
        [JsonProperty("forwardReciever")]
        public List<string> ForwardReciever { get; set; }

        [JsonProperty("forwardMessage")]
        public string ForwardMessage { get; set; }
    }
}
