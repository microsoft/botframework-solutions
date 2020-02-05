// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace EventHandler
{
    public class UserPreference
    {
        [JsonProperty(PropertyName = "id")]
        public string UserId { get; set; }

        public bool SendNotificationToConversation { get; set; }

        public bool SendNotificationToMobileDevice { get; set; }
    }
}