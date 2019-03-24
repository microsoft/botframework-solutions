using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Skills;

namespace CalendarSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        private readonly IAdapterIntegration _adapter;
        private readonly ISkillAdapter _skillAdapter;
        private readonly IBot _bot;

        public BotController(IAdapterIntegration adapter, ISkillAdapter skillAdapter, IBot bot)
            : base(adapter, skillAdapter, bot)
        {
            _adapter = adapter;
            _skillAdapter = skillAdapter;
            _bot = bot;
        }
    }
}