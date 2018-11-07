// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Assistant_WebTest.Models
{
    public class DirectlineResponse
    {
        public string conversationId { get; set; }
        public string token { get; set; }
        public int expires_in { get; set; }
    }
}
