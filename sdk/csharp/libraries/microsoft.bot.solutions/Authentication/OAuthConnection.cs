// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Solutions.Authentication
{
    [ExcludeFromCodeCoverageAttribute]
    public class OAuthConnection
    {
        public string Name { get; set; }

        public string Provider { get; set; }
    }
}