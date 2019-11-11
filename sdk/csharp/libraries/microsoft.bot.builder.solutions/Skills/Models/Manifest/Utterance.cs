// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest
{
    /// <summary>
    /// Utterances for a given locale which form part of an Action within a manifest.
    /// </summary>
    public class Utterance
    {
        public Utterance(string locale, string[] text)
        {
            Locale = locale;
            Text = text;
        }

        [JsonProperty(PropertyName = "locale")]
        public string Locale { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string[] Text { get; set; }
    }
}
