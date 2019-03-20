// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    [Route("api/skill")]
    public class SkillController : ControllerBase
    {
        private readonly IBot _bot;
        private readonly IAdapterIntegration _adapter;
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

        public SkillController(IAdapterIntegration adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        [HttpPost]
        public async Task PostAsync()
        {
            var activity = default(Activity);

            using (var bodyReader = new JsonTextReader(new StreamReader(Request.Body, Encoding.UTF8)))
            {
                activity = BotMessageSerializer.Deserialize<Activity>(bodyReader);
            }

            var invokeResponse = await _adapter.ProcessActivityAsync(
                Request.Headers["Authorization"],
                activity,
                _bot.OnTurnAsync,
                default(CancellationToken));

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
    }
}