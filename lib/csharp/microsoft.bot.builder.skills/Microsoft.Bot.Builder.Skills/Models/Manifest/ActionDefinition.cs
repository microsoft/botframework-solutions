using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// Definition of a Manifest Axtion. Describes how an actio is trigger and any slots (parameters) it accepts.
    /// </summary>
    public class ActionDefinition
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "slots")]
        public List<Slot> Slots { get; set; }

        [JsonProperty(PropertyName = "triggers")]
        public Triggers Triggers { get; set; }
    }
}
