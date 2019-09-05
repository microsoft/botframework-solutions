using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// Utterances for a given locale which form part of an Action within a manifest.
    /// </summary>
    public class Utterance
    {
        public Utterance(string locale, List<string> text)
        {
            Locale = locale;
            Text = text;
        }

        [JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }

        [JsonProperty(PropertyName = "text")]
        public List<string> Text { get; }
    }
}
