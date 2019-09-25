// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Auth;

namespace ITSMSkill.Utilities
{
    public class SimpleWhitelistAuthenticationProvider : IWhitelistAuthenticationProvider
    {
        // set VA appid here
        public HashSet<string> AppsWhitelist => new HashSet<string> { };
    }
}
