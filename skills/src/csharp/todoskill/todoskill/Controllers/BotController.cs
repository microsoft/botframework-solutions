﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Solutions;

namespace ToDoSkill.Controllers
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