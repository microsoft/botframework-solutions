using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;

namespace RestaurantBooking.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(IBotFrameworkHttpAdapter botFrameworkHttpAdapter, SkillAdapter skillAdapter, IBot bot)
            : base(botFrameworkHttpAdapter, skillAdapter, bot)
        {
        }
    }
}