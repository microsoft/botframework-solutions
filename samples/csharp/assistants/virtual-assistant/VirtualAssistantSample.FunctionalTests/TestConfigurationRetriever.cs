// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.FunctionalTests.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.IdentityModel.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.FunctionalTests
{
    public class TestConfigurationRetriever : IConfigurationRetriever<IDictionary<string, HashSet<string>>>
    {
        public IDictionary<string, HashSet<string>> EndorsementTable { get; } = new Dictionary<string, HashSet<string>>();

        public Task<IDictionary<string, HashSet<string>>> GetConfigurationAsync(string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            return Task.FromResult(EndorsementTable);
        }
    }
}
