using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using ServiceAdapter;
using VirtualAssistant.Adapters;

namespace VirtualAssistant.Controllers
{
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly CustomAdapter _customAdapter;
        private readonly IAdapterIntegration _botFrameworkAdapter;
        private readonly IBot _bot;
        private readonly JsonSerializer botMessageSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter> { new Iso8601TimeSpanConverter() },
        });

        public BotController(IAdapterIntegration botFrameworkAdapter, IEnumerable<IServiceAdapter> serviceAdapters, IBot bot)
        {
            _botFrameworkAdapter = botFrameworkAdapter;

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

        [HttpPost]
        public async Task PostAsync()
        {
            var activity = ReadRequest(Request);

            if (activity != null)
            {
                // grab the auth header from the inbound http request
                if (activity.ChannelId == CustomAdapter.CustomChannelId)
                {
                    var responseActivity = await _customAdapter.ProcessCustomChannelAsync(Request, activity, _bot.OnTurnAsync, default(CancellationToken));

                    WriteResponse(Response, responseActivity);
                }
                else
                {
                    var authHeader = Request.Headers["Authorization"];
                    var invokeResponse = await _botFrameworkAdapter.ProcessActivityAsync(authHeader, activity, _bot.OnTurnAsync, default(CancellationToken));

                    WriteResponse(Response, invokeResponse);
                }
            }
        }

        private Activity ReadRequest(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var activity = default(Activity);

            using (var bodyReader = new JsonTextReader(new StreamReader(request.Body, Encoding.UTF8)))
            {
                activity = botMessageSerializer.Deserialize<Activity>(bodyReader);
            }

            return activity;
        }

        private void WriteResponse(HttpResponse response, Activity activity)
        {
            using (var writer = new StreamWriter(response.Body))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    botMessageSerializer.Serialize(jsonWriter, activity);
                }
            }
        }

        private void WriteResponse(HttpResponse response, InvokeResponse invokeResponse)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (invokeResponse == null)
            {
                response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                response.ContentType = "application/json";
                response.StatusCode = invokeResponse.Status;

                using (var writer = new StreamWriter(response.Body))
                {
                    using (var jsonWriter = new JsonTextWriter(writer))
                    {
                        botMessageSerializer.Serialize(jsonWriter, invokeResponse.Body);
                    }
                }
            }
        }
    }
}