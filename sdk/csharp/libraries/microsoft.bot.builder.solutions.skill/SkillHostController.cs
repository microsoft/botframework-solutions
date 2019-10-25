using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;

namespace Microsoft.Bot.Builder.Solutions.Skill
{
    public abstract class SkillHostController : ControllerBase
    {
        private readonly IBot _bot;
        private readonly BotFrameworkHttpSkillsServer _skillServer;

        public SkillHostController(BotFrameworkHttpSkillsServer skillServer, IBot bot)
        {
            // adapter to use for calling back to channel
            _bot = bot;
            _skillServer = skillServer;
        }

        [Route("/v3/conversations/{*path}")]
        [HttpPost]
        [HttpGet]
        [HttpPut]
        [HttpDelete]
        public async Task ProcessAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await _skillServer.ProcessAsync(Request, Response, _bot);
        }
    }
}
