// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    /// This is the default Controller that contains APIs for handling
    /// calls from a channel and calls from a parent bot (to a skill bot).
    /// </summary>
    public abstract class SkillController : SkillControllerBase
    {
        public SkillController(
            IBot bot,
            BotSettingsBase botSettings,
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            ISkillWebSocketAdapter skillWebSocketAdapter,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
            : base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter, CreateWhiteListAuthenticator(botSettings, whitelistAuthenticationProvider))
        {
        }

        private static IAuthenticator CreateWhiteListAuthenticator(BotSettingsBase botSettings, IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
        {
            var whitelist = whitelistAuthenticationProvider ?? throw new ArgumentNullException(nameof(whitelistAuthenticationProvider));
            var authenticationProvider = new MsJWTAuthenticationProvider(botSettings.MicrosoftAppId);
            return new Authenticator(authenticationProvider, whitelist);
        }
    }
}