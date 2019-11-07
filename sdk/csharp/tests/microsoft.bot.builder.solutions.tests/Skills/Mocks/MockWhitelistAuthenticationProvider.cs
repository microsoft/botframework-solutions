// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills.Mocks
{
    public class MockWhitelistAuthenticationProvider : IWhitelistAuthenticationProvider
    {
        public HashSet<string> AppsWhitelist => new HashSet<string>();
    }
}