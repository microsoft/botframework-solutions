using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using ServiceAdapter;
using VirtualAssistant.Adapters;

namespace VirtualAssistant.Controllers
{
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly CustomAdapter _customAdapter;
        private readonly IBotFrameworkHttpAdapter _botFrameworkHttpAdapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter botFrameworkHttpAdapter, IEnumerable<IServiceAdapter> serviceAdapters, IBot bot)
        {
            _botFrameworkHttpAdapter = botFrameworkHttpAdapter;

            foreach (var adapter in serviceAdapters)
            {
                if (adapter.GetType() == typeof(CustomAdapter))
                {
                    _customAdapter = adapter as CustomAdapter;
                    break;
                }
            }

            _bot = bot;
        }

        [Route("api/messages")]
        [HttpPost]
        public async Task BotMessage()
        {
            await _botFrameworkHttpAdapter.ProcessAsync(Request, Response, _bot, default(CancellationToken));
        }

        [Route("api/custommessage")]
        [HttpPost]
        public async Task CustomMessage()
        {
            await _customAdapter.ProcessCustomChannelAsync(Request, Response, _bot.OnTurnAsync, default(CancellationToken));
        }
    }
}