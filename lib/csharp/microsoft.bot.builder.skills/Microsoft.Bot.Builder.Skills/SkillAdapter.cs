using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// Skill adapter provides the capability to invoke Skills (Bots) over a direct HTTP request.
    /// This requires the remote Skill to be leveraging this new adapter on a different MVC controller to the usual
    /// BotFrameworkAdapter that operates on the /api/messages route (DirectLine).
    /// </summary>
    public class SkillAdapter : BotAdapter, IAdapterIntegration, ISkillAdapter
    {
        private readonly ICredentialProvider _credentialProvider;
        private readonly IChannelProvider _channelProvider;
        private readonly ILogger _logger;
        private readonly Queue<Activity> queuedActivities = new Queue<Activity>();

        public SkillAdapter(ICredentialProvider credentialProvider = null, IChannelProvider channelProvider = null, ILogger<SkillAdapter> logger = null)
        {
            _credentialProvider = credentialProvider;
            _channelProvider = channelProvider;
            _logger = logger;
        }

        public async Task<InvokeResponse> ProcessActivityAsync(string authHeader, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // Ensure the Activity has been retrieved from the HTTP POST
            BotAssert.ActivityNotNull(activity);

            // Not performing authentication checks at this time

            //var claimsIdentity = await JwtTokenValidation.AuthenticateRequest(activity, authHeader, _credentialProvider, _channelProvider, _httpClient).ConfigureAwait(false);
            ClaimsIdentity claimsIdentity = null;
            return await ProcessActivityAsync(claimsIdentity, activity, callback, cancellationToken).ConfigureAwait(false);
        }

        public async Task<InvokeResponse> ProcessActivityAsync(ClaimsIdentity identity, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // Ensure the Activity has been retrieved from the HTTP POST
            BotAssert.ActivityNotNull(activity);

            // Process the Activity through the Middleware and the Bot, this will generate Activities which we need to send back.
            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, default(CancellationToken));
            }

            // TODO - This will be removed when we have async postbacks

            // Any Activity responses are now available (via SendActivitiesAsync) so we need to pass back for the response
            InvokeResponse response = new InvokeResponse();
            response.Status = (int)HttpStatusCode.OK;
            response.Body = GetReplies();

            return response;
        }

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
                    // TODO - Post to the Parent Bot ServiceURL
                    lock (queuedActivities)
                    {
                        queuedActivities.Enqueue(activity);
                    }
                }

                responses.Add(new ResourceResponse(activity.Id));
            }

            return responses.ToArray();
        }

        public List<Activity> GetReplies()
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

        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
