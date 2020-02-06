using Bot.Builder.Community.Adapters.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistantSample.Controllers
{
    [Route("api/google")]
    [ApiController]
    public class GoogleController : ControllerBase
    {
        private readonly GoogleAdapter Adapter;
        private readonly IBot Bot;

        public GoogleController(GoogleAdapter adapter, IBot bot)
        {
            Adapter = adapter;
            Bot = bot;
        }

        [HttpPost]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await Adapter.ProcessAsync(Request, Response, Bot);
        }
    }
}
