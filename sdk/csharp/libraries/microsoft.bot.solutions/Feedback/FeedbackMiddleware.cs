// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Feedback
{
    [Obsolete("FeedbackMiddleware will no longer work with any VA built with the 0.8 release or newer. For more information, refer to https://aka.ms/bfFeedbackDoc.", false)]
    public class FeedbackMiddleware : IMiddleware
    {
        private static FeedbackOptions _options;
        private static IStatePropertyAccessor<FeedbackRecord> _feedbackAccessor;
        private static ConversationState _conversationState;
        private IBotTelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackMiddleware"/> class.
        /// </summary>
        /// <param name="conversationState">The conversation state used for storing the feedback record before logging to Application Insights.</param>
        /// <param name="telemetryClient">The bot telemetry client used for logging the feedback record in Application Insights.</param>
        /// <param name="options">(Optional ) Feedback options object configuring the feedback actions and responses.</param>
        public FeedbackMiddleware(
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient,
            FeedbackOptions options = null)
        {
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _options = options ?? new FeedbackOptions();

            // Create FeedbackRecord state accessor
            _feedbackAccessor = conversationState.CreateProperty<FeedbackRecord>(nameof(FeedbackRecord));
        }

        /// <summary>
        /// Sends a Feedback Request activity with suggested actions to the user.
        /// </summary>
        /// <param name="context">Turn context for sending activities.</param>
        /// <param name="tag">Tag to categorize feedback record in Application Insights.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task RequestFeedbackAsync(ITurnContext context, string tag)
        {
            // clear state
            await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);

            // create feedbackRecord with original activity and tag
            var record = new FeedbackRecord()
            {
                Request = context.Activity,
                Tag = tag,
            };

            // store in state. No need to save changes, because its handled in IBot
            await _feedbackAccessor.SetAsync(context, record).ConfigureAwait(false);

            // If channel supports suggested actions
            if (Channel.SupportsSuggestedActions(context.Activity.ChannelId))
            {
                // prompt for feedback
                // if activity already had suggested actions, add the feedback actions
                if (context.Activity.SuggestedActions != null)
                {
                    var actions = new List<CardAction>()
                        .Concat(context.Activity.SuggestedActions.Actions)
                        .Concat(GetFeedbackActions())
                        .ToList();

                    await context.SendActivityAsync(MessageFactory.SuggestedActions(actions)).ConfigureAwait(false);
                }
                else
                {
                    var actions = GetFeedbackActions();
                    await context.SendActivityAsync(MessageFactory.SuggestedActions(actions)).ConfigureAwait(false);
                }
            }
            else
            {
                // else channel doesn't support suggested actions, so use hero card.
                var hero = new HeroCard(buttons: GetFeedbackActions());
                await context.SendActivityAsync(MessageFactory.Attachment(hero.ToAttachment())).ConfigureAwait(false);
            }
        }

        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            // get feedback record from state. If we don't find anything, set to null.
            var record = await _feedbackAccessor.GetAsync(context, () => null).ConfigureAwait(false);

            // if we have requested feedback
            if (record != null)
            {
                if (_options.FeedbackActions.Any(f => context.Activity.Text == (string)f.Value || context.Activity.Text == f.Title))
                {
                    // if activity text matches a feedback action
                    // save feedback in state
                    var feedback = _options.FeedbackActions
                        .Where(f => context.Activity.Text == (string)f.Value || context.Activity.Text == f.Title)
                        .First();

                    // Set the feedback to the action value for consistency
                    record.Feedback = (string)feedback.Value;
                    await _feedbackAccessor.SetAsync(context, record).ConfigureAwait(false);

                    if (_options.CommentsEnabled)
                    {
                        // if comments are enabled
                        // create comment prompt with dismiss action
                        if (Channel.SupportsSuggestedActions(context.Activity.ChannelId))
                        {
                            var commentPrompt = MessageFactory.SuggestedActions(
                                text: $"{_options.FeedbackReceivedMessage} {_options.CommentPrompt}",
                                cardActions: new List<CardAction>() { _options.DismissAction });

                            // prompt for comment
                            await context.SendActivityAsync(commentPrompt).ConfigureAwait(false);
                        }
                        else
                        {
                            // channel doesn't support suggestedActions, so use hero card.
                            var hero = new HeroCard(
                                text: _options.CommentPrompt,
                                buttons: new List<CardAction> { _options.DismissAction });

                            // prompt for comment
                            await context.SendActivityAsync(MessageFactory.Attachment(hero.ToAttachment())).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // comments not enabled, respond and cleanup
                        // send feedback response
                        await context.SendActivityAsync(_options.FeedbackReceivedMessage).ConfigureAwait(false);

                        // log feedback in appInsights
                        FeedbackHelper.LogFeedback(record, _telemetryClient);

                        // clear state
                        await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                    }
                }
                else if (context.Activity.Text == (string)_options.DismissAction.Value || context.Activity.Text == _options.DismissAction.Title)
                {
                    // if user dismissed
                    // log existing feedback
                    if (!string.IsNullOrEmpty(record.Feedback))
                    {
                        // log feedback in appInsights
                        FeedbackHelper.LogFeedback(record, _telemetryClient);
                    }

                    // clear state
                    await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(record.Feedback) && _options.CommentsEnabled)
                {
                    // if we received a comment and user didn't dismiss
                    // store comment in state
                    record.Comment = context.Activity.Text;
                    await _feedbackAccessor.SetAsync(context, record).ConfigureAwait(false);

                    // Respond to comment
                    await context.SendActivityAsync(_options.CommentReceivedMessage).ConfigureAwait(false);

                    // log feedback in appInsights
                    FeedbackHelper.LogFeedback(record, _telemetryClient);

                    // clear state
                    await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                }
                else
                {
                    // we requested feedback, but the user responded with something else
                    // clear state and continue (so message can be handled by dialog stack)
                    await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                    await next(cancellationToken).ConfigureAwait(false);
                }

                await _conversationState.SaveChangesAsync(context).ConfigureAwait(false);
            }
            else
            {
                // We are not requesting feedback. Go to next.
                await next(cancellationToken).ConfigureAwait(false);
            }
        }

        private static List<CardAction> GetFeedbackActions()
        {
            var actions = new List<CardAction>(_options.FeedbackActions)
            {
                _options.DismissAction,
            };
            return actions;
        }
    }
}
