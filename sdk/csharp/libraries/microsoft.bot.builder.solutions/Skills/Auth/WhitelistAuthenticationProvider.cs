// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    /// <summary>
    /// Loads the apps whitelist from settings.
    /// </summary>
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class WhitelistAuthenticationProvider : IWhitelistAuthenticationProvider
    {
        public WhitelistAuthenticationProvider(IConfiguration configuration, string whitelistProperty = "skillAuthenticationWhitelist")
        {
            // skillAuthenticationWhitelist is the setting in appsettings.json file
            // that conists of the list of parent bot ids that are allowed to access the skill
            // to add a new parent bot simply go to the skillAuthenticationWhitelist and add
            // the parent bot's microsoft app id to the list
            var section = configuration.GetSection(whitelistProperty);
            var appsList = section.Get<string[]>();
            AppsWhitelist = appsList != null ? new HashSet<string>(appsList) : null;
        }

        public HashSet<string> AppsWhitelist { get; }
    }
}
