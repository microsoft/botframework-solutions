using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Skills;

namespace AutomotiveSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(IAdapterIntegration adapter, ISkillAdapter skillAdapter, IBot bot)
            : base(adapter, skillAdapter, bot)
        {
        }
    }
}