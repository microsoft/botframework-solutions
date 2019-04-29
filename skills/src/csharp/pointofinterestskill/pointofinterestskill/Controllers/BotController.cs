using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions;

namespace PointOfInterestSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            SkillHttpAdapter skillHttpAdapter,
            SkillWebSocketAdapter skillWebSocketAdapter,
            IBot bot,
            BotSettingsBase botSettings)
            : base(botFrameworkHttpAdapter, skillHttpAdapter, skillWebSocketAdapter, bot, botSettings)
        {
        }
    }
}