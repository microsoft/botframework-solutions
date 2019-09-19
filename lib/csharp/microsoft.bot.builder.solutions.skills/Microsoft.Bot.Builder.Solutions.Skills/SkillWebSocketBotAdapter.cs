// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.StreamingExtensions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Builder.Solutions.Skills
{
    /// <summary>
    /// This adapter is responsible for processing incoming activity from a bot-to-bot call over websocket transport.
    /// It'll perform the following tasks:
    /// 1. Process the incoming activity by calling into pipeline.
    /// 2. Implement BotAdapter protocol. Each method will send the activity back to calling bot using websocket.
    /// </summary>
    public class SkillWebSocketBotAdapter
        : WebSocketEnabledHttpAdapter, IRemoteUserTokenProvider, IFallbackRequestProvider
    {
        private readonly IBotTelemetryClient _botTelemetryClient;

        public SkillWebSocketBotAdapter(IConfiguration configuration, IMiddleware middleware = null, IBotTelemetryClient botTelemetryClient = null)
            : base(configuration, null, null, null, null)
        {
            _botTelemetryClient = botTelemetryClient ?? NullBotTelemetryClient.Instance;

            if (middleware != null)
            {
                Use(middleware);
            }
        }

        public async Task SendRemoteTokenRequestEventAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
            var response = turnContext.Activity.CreateReply();
            response.Type = ActivityTypes.Event;
            response.Name = TokenEvents.TokenRequestEventName;

            // set SemanticAction property of the activity properly
            EnsureActivitySemanticAction(turnContext, response);

            // Send the tokens/request Event
            await SendActivitiesAsync(turnContext, new[] { response }, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendRemoteFallbackEventAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // We trigger a Fallback Request from the Parent Bot by sending a "skill/fallbackRequest" event
            var response = turnContext.Activity.CreateReply();
            response.Type = ActivityTypes.Event;
            response.Name = SkillEvents.FallbackEventName;

            // set SemanticAction property of the activity properly
            EnsureActivitySemanticAction(turnContext, response);

            // Send the fallback Event
            await SendActivitiesAsync(turnContext, new[] { response }, cancellationToken).ConfigureAwait(false);
        }

        private void EnsureActivitySemanticAction(ITurnContext turnContext, Activity activity)
        {
            if (activity == null || turnContext?.Activity == null)
            {
                return;
            }

            // set state of semantic action based on the activity type
            if (activity.Type != ActivityTypes.Trace
                && turnContext.Activity.SemanticAction != null
                && !string.IsNullOrWhiteSpace(turnContext.Activity.SemanticAction.Id))
            {
                // if Skill's dialog didn't set SemanticAction property
                // simply copy over from the incoming activity
                if (activity.SemanticAction == null)
                {
                    activity.SemanticAction = turnContext.Activity.SemanticAction;
                }

                if (activity.Type == ActivityTypes.EndOfConversation)
                {
                    activity.SemanticAction.State = SkillConstants.SkillDone;
                }
                else
                {
                    activity.SemanticAction.State = SkillConstants.SkillContinue;
                }
            }
        }
    }
}
