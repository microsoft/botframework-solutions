// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Extensions.Configuration;

namespace $safeprojectname$
{
    /// <summary>
    /// Loads the apps whitelist from settings.
    /// </summary>
    public class WhiteListAuthProvider : IWhitelistAuthenticationProvider
    {
        public WhiteListAuthProvider(IConfiguration configuration)
        {
            // skillAuthenticationWhitelist is the setting in appsettings.json file
            // that conists of the list of parent bot ids that are allowed to access the skill
            // to add a new parent bot simply go to the skillAuthenticationWhitelist and add
            // the parent bot's microsoft app id to the list
            var section = configuration.GetSection($"skillAuthenticationWhitelist");
            var appsList = section.Get<string[]>();
            AppsWhitelist = appsList != null ? new HashSet<string>(appsList) : null;
        }

        public HashSet<string> AppsWhitelist { get; }
    }
}