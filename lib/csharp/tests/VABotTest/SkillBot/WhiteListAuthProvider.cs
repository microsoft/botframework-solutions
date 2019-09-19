// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Extensions.Configuration;

namespace SkillBot
{
    /// <summary>
    /// Loads the apps whitelist from settings.
    /// </summary>
    public class WhiteListAuthProvider : IWhitelistAuthenticationProvider
    {
        public WhiteListAuthProvider(IConfiguration configuration)
        {
            var section = configuration.GetSection($"appsWhitelist");
            var appsList = section.Get<string[]>();
            AppsWhitelist = new HashSet<string>(appsList);
        }

        public HashSet<string> AppsWhitelist { get; }
    }
}
