// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// Slot definition for a given Action within a Skill manifest.
    /// </summary>
    public class Slot
    {
        public Slot()
        {
        }

        public Slot(string name, List<string> types)
        {
            Name = name;
            Types.AddRange(types);
        }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "types")]
        public List<string> Types { get; } = new List<string>();
    }
}
