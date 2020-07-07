// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillSample.FunctionalTests.Dialogs
{
    public class SampleActionInput
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
