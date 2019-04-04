using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Auth;

namespace PointOfInterestSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
<<<<<<< HEAD
        public BotController(IServiceProvider serviceProvider)
            : base(serviceProvider)
=======
        public BotController(IBotFrameworkHttpAdapter botFrameworkHttpAdapter, SkillAdapter skillAdapter, ISkillAuthProvider skillAuthProvider, IBot bot)
            : base(botFrameworkHttpAdapter, skillAdapter, skillAuthProvider, bot)
>>>>>>> origin/4.4
        {
        }
    }
}