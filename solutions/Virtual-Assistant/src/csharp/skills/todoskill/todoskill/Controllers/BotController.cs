using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Shared;

namespace ToDoSkill.Controllers
{
    [ApiController]
    public class BotController : SkillController
    {
        public BotController(IServiceProvider serviceProvider, BotSettingsBase botSettings)
            : base(serviceProvider, botSettings)
        {
        }
    }
}