// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Schema;

namespace Assistant_WebTest.Models
{
    public class LinkedAccountsViewModel
    {
        public string UserId { get; set; }
        public TokenStatus[] Status { get; set; }

        public string DirectLineToken { get; set; }

        public string Endpoint { get; set; }
    }
}
