using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;

namespace WeatherSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(
            IBot bot,
            BotSettingsBase botSettings,
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            SkillWebSocketAdapter skillWebSocketAdapter,
            SkillHttpAdapter skillHttpAdapter)
            : base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter, skillHttpAdapter)
        {
        }
    }
}