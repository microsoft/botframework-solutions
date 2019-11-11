// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public interface IWhitelistAuthenticationProvider
    {
        HashSet<string> AppsWhitelist { get; }
    }
}