using Newtonsoft.Json;
using System;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{

    public class Action
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "definition")]
        public ActionDefinition Definition { get; set; }
    }
}
