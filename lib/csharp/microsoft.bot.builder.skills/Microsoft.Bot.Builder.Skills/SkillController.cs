// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// This is the default Controller that contains APIs for handling
    /// calls from a channel and
    /// calls from a parent bot (to a skill bot)
    /// </summary>
    [ApiController]
    public abstract class SkillController : ControllerBase
    {
        private readonly IBot _bot;
        private readonly IBotFrameworkHttpAdapter _botFrameworkHttpAdapter;
        private readonly SkillAdapter _skillAdapter;

        public SkillController(IBotFrameworkHttpAdapter botFrameworkHttpAdapter, SkillAdapter skillAdapter, IBot bot)
        {
            _botFrameworkHttpAdapter = botFrameworkHttpAdapter;
            _skillAdapter = skillAdapter;
            _bot = bot;
        }

        /// <summary>
        /// This API is the endpoint for when a bot receives a message from a channel or a parent bot
        /// </summary>
        /// <returns></returns>
        [Route("api/messages")]
        [HttpPost]
        public async Task BotMessage()
        {
            await _botFrameworkHttpAdapter.ProcessAsync(Request, Response, _bot, default(CancellationToken));
        }

        /// <summary>
        /// This API is the endpoint the bot exposes as skill
        /// </summary>
        /// <returns></returns>
        [Route("api/skill/messages")]
        [HttpPost]
        public async Task SkillMessage()
        {
            await _skillAdapter.ProcessAsync(Request, Response, _bot, default(CancellationToken));
        }        
    }
}