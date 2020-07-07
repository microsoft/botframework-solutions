// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualAssistantSample.Models
{
    /// <summary>
    /// Skill Card action data should contain skillName parameter
    /// This class is used to deserialize it and get skillName.
    /// </summary>
    /// <value>
    /// SkillName.
    /// </value>
    public class SkillCardActionData
    {
        [JsonProperty("SkillId")]
        public string SkillId { get; set; }
    }
}
