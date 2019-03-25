// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;
using Microsoft.Rest.Serialization;
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
        private readonly IAdapterIntegration _adapter;
        private readonly ISkillAdapter _skillAdapter;
        public static readonly JsonSerializer BotMessageSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter> { new Iso8601TimeSpanConverter() },
        });

        public SkillController(IAdapterIntegration adapter, ISkillAdapter skillAdapter, IBot bot)
        {
            _adapter = adapter;
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
            var activity = default(Activity);

            using (var bodyReader = new JsonTextReader(new StreamReader(Request.Body, Encoding.UTF8)))
            {
                activity = BotMessageSerializer.Deserialize<Activity>(bodyReader);
            }

            InvokeResponse invokeResponse;
            if (Request.Headers.ContainsKey("skill") && Request.Headers["skill"].ToString().Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                invokeResponse = await _skillAdapter.ProcessActivityAsync(
                    Request.Headers["Authorization"],
                    activity,
                    _bot.OnTurnAsync,
                    default(CancellationToken));
            }
            else
            {
                invokeResponse = await ProcessAsync(Request.Headers["Authorization"], activity, _bot.OnTurnAsync, default(CancellationToken));
            }

            if (invokeResponse == null)
            {
                Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                Response.ContentType = "application/json";
                Response.StatusCode = invokeResponse.Status;

                using (var writer = new StreamWriter(Response.Body))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        BotMessageSerializer.Serialize(jsonWriter, invokeResponse.Body);
                    }
                }
            }
        }

        /// <summary>
        /// This API is the endpoint for when a bot receives a callback from a skill bot
        /// </summary>
        /// <returns></returns>
        [Route("api/skills")]
        [HttpPost]
        public async Task SkillMessage()
        {
            // authenticate the request from a skill bot
            // by looking at the skill header which should be
            // a previously issued key from this bot

            // look up the previous conversation to find the serviceUrl
            // to call back to when the handling of this call is finished
        }

        /// <summary>
        /// This is the method that handles the regular calls except for skill calls
        /// </summary>
        /// <param name="authHeader">auth header.</param>
        /// <param name="activity">activity object.</param>
        /// <param name="callback">bot callback</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>invoke response.</returns>
        protected virtual async Task<InvokeResponse> ProcessAsync(string authHeader, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            return await _adapter.ProcessActivityAsync(authHeader, activity, callback, cancellationToken);
        }
    }
}