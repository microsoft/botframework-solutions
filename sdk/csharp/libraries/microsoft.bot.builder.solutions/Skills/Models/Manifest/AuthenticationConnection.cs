﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest
{
    /// <summary>
    /// Describes an Authentication connection that a Skill requires for operation.
    /// </summary>
    public class AuthenticationConnection
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "serviceProviderId")]
        public string ServiceProviderId { get; set; }

        [JsonProperty(PropertyName = "scopes")]
        public string Scopes { get; set; }
    }
}
