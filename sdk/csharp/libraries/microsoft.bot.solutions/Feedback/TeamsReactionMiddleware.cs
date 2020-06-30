using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Feedback
{
    public class TeamsReactionMiddleware : IMiddleware
    {
        private const string TeamsFeedback = "TeamsReaction";
        private readonly IBotTelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsReactionMiddleware"/> class.
        /// </summary>
        /// <param name="telemetryClient">The bot telemetry client used for logging the feedback record in Application Insights.</param>
        public TeamsReactionMiddleware(IBotTelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            // If Teams Reaction Log Feedback
            if (turnContext.Activity.ChannelId.Equals(Channels.Msteams, StringComparison.OrdinalIgnoreCase)
                && turnContext.Activity.Type != null
                && turnContext.Activity.Type == ActivityTypes.MessageReaction)
            {
                IList<MessageReaction> reactionsAdded = turnContext.Activity.ReactionsAdded;
                var record = new FeedbackRecord
                {
                    TeamsReaction = reactionsAdded[reactionsAdded.Count - 1].Type,
                    Tag = TeamsReactionMiddleware.TeamsFeedback,
                    Request = turnContext.Activity,
                };

                FeedbackHelper.LogFeedback(record, _telemetryClient);
            }

            await next(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
