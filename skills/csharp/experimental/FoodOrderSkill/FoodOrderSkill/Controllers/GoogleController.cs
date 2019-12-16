using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Builder.Community.Adapters.Google.Integration.AspNet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;

namespace FoodOrderSkill.Controllers
{
    [Route("api/actionrequests")]
    [ApiController]
    public class GoogleController : ControllerBase
    {
        private readonly IGoogleHttpAdapter _adapter;
        private readonly IBot _bot;

        public GoogleController(IGoogleHttpAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        [HttpPost]
        public async Task PostAsync()
        {
            await _adapter.ProcessAsync(Request, Response, _bot);
        }
    }
}