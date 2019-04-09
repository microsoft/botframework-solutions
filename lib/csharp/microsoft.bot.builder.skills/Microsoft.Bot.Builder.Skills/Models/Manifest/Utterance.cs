using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// Utterances for a given locale which form part of an Action within a manifest.
    /// </summary>
    public class Utterance
    {
        [JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string[] Text { get; set; }
    }
}
