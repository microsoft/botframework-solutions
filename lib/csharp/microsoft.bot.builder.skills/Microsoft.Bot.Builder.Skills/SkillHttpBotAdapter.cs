using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// This adapter is responsible for processing incoming activity from a bot-to-bot call over http transport.
    /// It'll performa the following tasks:
    /// 1. Process the incoming activity by calling into pipeline.
    /// 2. Implement BotAdapter protocol.
    ///     a). SendActivitiesAsync: This will buffer all responses and send them back as response in one batch.
    ///     b). UpdateActivityAsync, DeleteActivityAsync: not supported for Http model.
    /// </summary>
    public class SkillHttpBotAdapter : BotAdapter, IActivityHandler, IRemoteUserTokenProvider
    {
        private readonly IBotTelemetryClient _botTelemetryClient;
        private readonly Queue<Activity> queuedActivities = new Queue<Activity>();

        public SkillHttpBotAdapter(IBotTelemetryClient botTelemetryClient = null)
        {
            _botTelemetryClient = botTelemetryClient ?? NullBotTelemetryClient.Instance;
        }

        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext context, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();

            foreach (var activity in activities)
            {
                if (string.IsNullOrWhiteSpace(activity.Id))
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
            throw new NotSupportedException("Http Request/Response model doesn't support DeleteActivityAsync call!");
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Http Request/Response model doesn't support UpdateActivityAsync call!");
        }

        public override Task ContinueConversationAsync(string botId, ConversationReference reference, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Http Request/Response model doesn't support ContinueConversationAsync call!");
        }

        /// <summary>
        /// Primary adapter method for processing activities sent from calling bot.
        /// </summary>
        /// <param name="activity">The activity to process.</param>
        /// <param name="callback">The BotCallBackHandler to call on completion.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response to the activity.</returns>
        public async Task<InvokeResponse> ProcessActivityAsync(Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // Ensure the Activity has been retrieved from the HTTP POST
            BotAssert.ActivityNotNull(activity);

            _botTelemetryClient.TrackTrace($"SkillHttpBotAdapter: Received an incoming activity. Activity id: {activity.Id}", Severity.Information, null);

            // Process the Activity through the Middleware and the Bot, this will generate Activities which we need to send back.
            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, default(CancellationToken));
            }

            _botTelemetryClient.TrackTrace($"SkillHttpBotAdapter: Batching activities in the response. ReplyToId: {activity.ReplyToId}", Severity.Information, null);

            // Any Activity responses are now available (via SendActivitiesAsync) so we need to pass back for the response
            var response = new InvokeResponse
            {
                Status = (int)HttpStatusCode.OK,
                Body = GetReplies(),
            };

            return response;
        }

        public async Task SendRemoteTokenRequestEventAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
            var response = turnContext.Activity.CreateReply();
            response.Type = ActivityTypes.Event;
            response.Name = "tokens/request";

            // Send the tokens/request Event
            await SendActivitiesAsync(turnContext, new Activity[] { response }, cancellationToken);
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
    }
}