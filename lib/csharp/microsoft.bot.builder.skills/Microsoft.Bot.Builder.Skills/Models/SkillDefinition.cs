using System.Collections.Generic;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillDefinition : ConnectedService
    {
        public SkillDefinition()
            : base("skill")
        {
        }

        [JsonProperty("dispatchIntent")]
        public string DispatchIntent { get; set; }

        [JsonProperty("assembly")]
        public string Endpoint { get; set; }    

        [JsonProperty("supportedProviders")]
        public string[] SupportedProviders { get; set; }
    }
}
