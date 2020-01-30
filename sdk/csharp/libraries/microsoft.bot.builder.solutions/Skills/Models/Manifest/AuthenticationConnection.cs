// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest
{
    /// <summary>
    /// Describes an Authentication connection that a Skill requires for operation.
    /// </summary>
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
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
