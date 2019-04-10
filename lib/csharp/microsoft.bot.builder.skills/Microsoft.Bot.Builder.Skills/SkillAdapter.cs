using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// Skill adapter provides the capability to invoke Skills (Bots) over a direct HTTP request.
    /// This requires the remote Skill to be leveraging this new adapter on a different MVC controller to the usual
    /// BotFrameworkAdapter that operates on the /api/messages route (DirectLine).
    /// </summary>
    public class SkillAdapter : BotAdapter, IBotFrameworkHttpAdapter, IRemoteUserTokenProvider
    {
        private readonly ICredentialProvider _credentialProvider;
        private readonly ILogger _logger;
        private readonly Queue<Activity> queuedActivities = new Queue<Activity>();
        private readonly JsonSerializer BotMessageSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            ContractResolver = new ReadOnlyJsonContractResolver(),
            Converters = new List<JsonConverter> { new Iso8601TimeSpanConverter() },
        });

        public SkillAdapter(ICredentialProvider credentialProvider = null, ILogger<SkillAdapter> logger = null)
        {
            _credentialProvider = credentialProvider;
            _logger = logger;
        }

        public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            // deserialize the incoming Activity
            var activity = ReadRequest(httpRequest);

            // grab the auth header from the inbound http request
            var authHeader = httpRequest.Headers["Authorization"];

            // process the inbound activity with the bot
            var invokeResponse = await ProcessActivityAsync(authHeader, activity, bot.OnTurnAsync, cancellationToken).ConfigureAwait(false);

            // write the response, potentially serializing the InvokeResponse
            WriteResponse(httpResponse, invokeResponse);
        }

        /// <summary>
        /// Continue the conversation by passing the activity through the pipeline.
        /// </summary>
        /// <param name="botId"></param>
        /// <param name="reference"></param>
        /// <param name="callback"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(botId))
            {
                throw new ArgumentNullException(nameof(botId));
            }

            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            using (var context = new TurnContext(this, reference.GetContinuationActivity()))
            {
                await RunPipelineAsync(context, callback, cancellationToken);
            }
        }

        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext context, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();
            var proactiveActivities = new List<Activity>();

            foreach (var activity in activities)
            {
                if (string.IsNullOrEmpty(activity.Id))
                {
                    activity.Id = Guid.NewGuid().ToString("n");
                }

                if (activity.Timestamp == null)
                {
                    activity.Timestamp = DateTime.UtcNow;
                }

                if (activity.Type == ActivityTypesEx.Delay)
                {
                    // The BotFrameworkAdapter and Console adapter implement this
                    // hack directly in the POST method. Replicating that here
                    // to keep the behavior as close as possible to facillitate
                    // more realistic tests.
                    var delayMs = (int)activity.Value;
                    await Task.Delay(delayMs);
                }
                else if (activity.Type == ActivityTypes.Trace && activity.ChannelId != "emulator")
                {
                    // if it is a Trace activity we only send to the channel if it's the emulator.
                }
                else if (activity.Type == ActivityTypes.Typing && activity.ChannelId == "test")
                {
                    // If it's a typing activity we omit this in test scenarios to avoid test failures
                }
                else
                {
                    // Queue up this activity for aggregation back to the calling Bot in one overall message.
                    lock (queuedActivities)
                    {
                        queuedActivities.Enqueue(activity);
                    }
                }

                responses.Add(new ResourceResponse(activity.Id));
            }

            return responses.ToArray();
        }

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task<InvokeResponse> ProcessActivityAsync(string authHeader, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // Ensure the Activity has been retrieved from the HTTP POST
            BotAssert.ActivityNotNull(activity);

            // Not performing authentication checks at this time

            //var claimsIdentity = await JwtTokenValidation.AuthenticateRequest(activity, authHeader, _credentialProvider, _channelProvider, _httpClient).ConfigureAwait(false);
            ClaimsIdentity claimsIdentity = null;
            return await ProcessActivityAsync(claimsIdentity, activity, callback, cancellationToken).ConfigureAwait(false);
        }

        private async Task<InvokeResponse> ProcessActivityAsync(ClaimsIdentity identity, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // Ensure the Activity has been retrieved from the HTTP POST
            BotAssert.ActivityNotNull(activity);

            // Process the Activity through the Middleware and the Bot, this will generate Activities which we need to send back.
            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, default(CancellationToken));
            }

            // Any Activity responses are now available (via SendActivitiesAsync) so we need to pass back for the response
            InvokeResponse response = new InvokeResponse();
            response.Status = (int)HttpStatusCode.OK;
            response.Body = GetReplies();

            return response;
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
                activity = BotMessageSerializer.Deserialize<Activity>(bodyReader);
            }

            return activity;
        }

        private List<Activity> GetReplies()
        {
            var replies = new List<Activity>();

            lock (queuedActivities)
            {
                if (queuedActivities.Count > 0)
                {
                    var count = queuedActivities.Count;
                    for (var i = 0; i < count; ++i)
                    {
                        replies.Add(queuedActivities.Dequeue());
                    }
                }
            }

            return replies;
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
                        BotMessageSerializer.Serialize(jsonWriter, invokeResponse.Body);
                    }
                }
            }
        }

        public async Task SendRemoteTokenRequestEvent(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
            // TODO Error handling - if we get a new activity that isn't an event
            var response = turnContext.Activity.CreateReply();
            response.Type = ActivityTypes.Event;
            response.Name = "tokens/request";

            // Send the tokens/request Event
            await SendActivitiesAsync(turnContext, new Activity[] { response }, cancellationToken);
        }
    }
}