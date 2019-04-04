using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Skills;

namespace EmailSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }
    }
}