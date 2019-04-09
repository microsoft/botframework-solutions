using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// Definition of the triggers for a given action within a Skill.
    /// </summary>
    public class Triggers
    {
        [JsonProperty(PropertyName = "utterances")]
        public Utterance[] Utterances { get; set; }

        [JsonProperty(PropertyName = "utteranceSources")]
        public UtteranceSource[] UtteranceSources { get; set; }

        [JsonProperty(PropertyName = "events")]
        public Event[] Events { get; set; }
    }
}
