using System.Collections.Generic;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Skills
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
        public string Assembly { get; set; }

        [JsonProperty("luisServiceIds")]
        public string[] LuisServiceIds { get; set; }

        [JsonProperty("supportedProviders")]
        public string[] SupportedProviders { get; set; }

        [JsonProperty("parameters")]
        public string[] Parameters { get; set; }

        [JsonProperty("configuration")]
        public Dictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();

        public override void Decrypt(string secret)
        {
            base.Decrypt(secret);
        }

        public override void Encrypt(string secret)
        {
            base.Encrypt(secret);
        }
    }
}
