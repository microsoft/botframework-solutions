// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace LinkedAccounts.Web.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Schema;

    public class LinkedAccountsViewModel
    {
        public string UserId { get; set; }
        public TokenStatus[] Status { get; set; }

        public string DirectLineToken { get; set; }
        
        public string Endpoint { get; set; }
    }
}
