using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Protocol;
using Microsoft.Bot.Protocol.WebSockets;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// This adapter is responsible for processing incoming activity from a bot-to-bot call over websocket transport.
    /// It'll performa the following tasks:
    /// 1. Process the incoming activity by calling into pipeline.
    /// 2. Implement BotAdapter protocol. Each method will send the activity back to calling bot using websocket.
    /// </summary>
    public class SkillWebSocketBotAdapter : BotAdapter, IActivityHandler, IRemoteUserTokenProvider
    {
        private readonly ILogger _logger;

        public SkillWebSocketBotAdapter(
            IMiddleware middleware = null,
            ILogger logger = null)
        {
            _logger = logger ?? NullLogger.Instance;

            if (middleware != null)
            {
                Use(middleware);
            }
        }

        public WebSocketServer Server { get; set; }

        /// <summary>
        /// Primary adapter method for processing activities sent from calling bot.
        /// </summary>
        /// <param name="activity">The activity to process.</param>
        /// <param name="callback">The BotCallBackHandler to call on completion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response to the activity.</returns>
        public async Task<InvokeResponse> ProcessActivityAsync(Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            BotAssert.ActivityNotNull(activity);

            _logger.LogInformation($"Received an incoming activity.  ActivityId: {activity.Id}");

            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, cancellationToken).ConfigureAwait(false);

                // We do not support Invoke in websocket transport
                if (activity.Type == ActivityTypes.Invoke)
                {
                    return new InvokeResponse { Status = (int)HttpStatusCode.NotImplemented };
                }

                return null;
            }
        }

        /// <summary>
        /// Sends activities to the conversation.
        /// </summary>
        /// <param name="turnContext">The context object for the turn.</param>
        /// <param name="activities">The activities to send.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>If the activities are successfully sent, the task result contains
        /// an array of <see cref="ResourceResponse"/> objects containing the IDs that
        /// the receiving channel assigned to the activities.</remarks>
        /// <seealso cref="ITurnContext.OnSendActivities(SendActivitiesHandler)"/>
        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (activities == null)
            {
                throw new ArgumentNullException(nameof(activities));
            }

            if (activities.Length == 0)
            {
                throw new ArgumentException("Expecting one or more activities, but the array was empty.", nameof(activities));
            }

            var responses = new ResourceResponse[activities.Length];

            /*
             * NOTE: we're using for here (vs. foreach) because we want to simultaneously index into the
             * activities array to get the activity to process as well as use that index to assign
             * the response to the responses array and this is the most cost effective way to do that.
             */
            for (var index = 0; index < activities.Length; index++)
            {
                var activity = activities[index];
                if (string.IsNullOrWhiteSpace(activity.Id))
                {
                    activity.Id = Guid.NewGuid().ToString("n");
                }

                var response = default(ResourceResponse);
                _logger.LogInformation($"Sending activity.  ReplyToId: {activity.ReplyToId}");

                if (activity.Type == ActivityTypesEx.Delay)
                {
                    // The Activity Schema doesn't have a delay type build in, so it's simulated
                    // here in the Bot. This matches the behavior in the Node connector.
                    var delayMs = (int)activity.Value;
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

                    // No need to create a response. One will be created below.
                }

                if (activity.Type != ActivityTypes.Trace ||
                    (activity.Type == ActivityTypes.Trace && activity.ChannelId == "emulator"))
                {
                    var requestPath = $"/activities/{activity.Id}";
                    var request = Request.CreatePost(requestPath);
                    request.SetBody(activity);
                    response = await SendRequestAsync<ResourceResponse>(request).ConfigureAwait(false);
                }

                // If No response is set, then defult to a "simple" response. This can't really be done
                // above, as there are cases where the ReplyTo/SendTo methods will also return null
                // (See below) so the check has to happen here.

                // Note: In addition to the Invoke / Delay / Activity cases, this code also applies
                // with Skype and Teams with regards to typing events.  When sending a typing event in
                // these _channels they do not return a RequestResponse which causes the bot to blow up.
                // https://github.com/Microsoft/botbuilder-dotnet/issues/460
                // bug report : https://github.com/Microsoft/botbuilder-dotnet/issues/465
                if (response == null)
                {
                    response = new ResourceResponse(activity.Id ?? string.Empty);
                }

                responses[index] = response;
            }

            return responses;
        }

        public override async Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            var requestPath = $"/activities/{activity.Id}";
            var request = Request.CreatePut(requestPath);
            request.SetBody(activity);
            return await SendRequestAsync<ResourceResponse>(request, cancellationToken).ConfigureAwait(false);
        }

        public override async Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            var requestPath = $"/activities/{reference.ActivityId}";
            var request = Request.CreateDelete(requestPath);
            await SendRequestAsync(request, cancellationToken);
        }

        public async Task SendRemoteTokenRequestEvent(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
            var response = turnContext.Activity.CreateReply();
            response.Type = ActivityTypes.Event;
            response.Name = TokenEvents.TokenRequestEventName;

            // Send the tokens/request Event
            await SendActivitiesAsync(turnContext, new Activity[] { response }, cancellationToken);
        }

        private async Task<T> SendRequestAsync<T>(Request request, CancellationToken cancellation = default(CancellationToken))
        {
            try
            {
                var serverResponse = await this.Server.SendAsync(request, cancellation).ConfigureAwait(false);

                if (serverResponse.StatusCode == (int)HttpStatusCode.OK)
                {
                    return serverResponse.ReadBodyAsJson<T>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                throw ex;
            }

            return default(T);
        }

        private async Task SendRequestAsync(Request request, CancellationToken cancellation = default(CancellationToken))
        {
            try
            {
                var serverResponse = await this.Server.SendAsync(request, cancellation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);

                throw ex;
            }
        }
    }
}