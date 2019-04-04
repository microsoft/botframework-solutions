using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Auth;

namespace ToDoSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(IBotFrameworkHttpAdapter botFrameworkHttpAdapter, SkillAdapter skillAdapter, ISkillAuthProvider skillAuthProvider, IBot bot)
            : base(botFrameworkHttpAdapter, skillAdapter, skillAuthProvider, bot)
        {
        }
    }
}