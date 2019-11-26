// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace AutomotiveSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(
            IBot bot,
            BotSettingsBase botSettings,
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            SkillWebSocketAdapter skillWebSocketAdapter,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
            : base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter, whitelistAuthenticationProvider)
        {
        }
    }
}