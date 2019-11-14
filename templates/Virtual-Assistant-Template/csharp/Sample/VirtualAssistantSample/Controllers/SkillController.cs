using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Solutions.Skills;

namespace VirtualAssistantSample.Controllers
{
    [ApiController]
    public class SkillController : SkillHostController
    {
        public SkillController(BotFrameworkHttpSkillsServer botFrameworkHttpSkillsServer, IBot bot)
            : base(botFrameworkHttpSkillsServer, bot)
        { }
    }
}
