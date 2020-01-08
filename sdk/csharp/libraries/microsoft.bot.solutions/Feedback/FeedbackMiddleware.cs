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
    public class FeedbackMiddleware : IMiddleware
    {
        private static FeedbackOptions _options;
        private static IStatePropertyAccessor<FeedbackRecord> _feedbackAccessor;
        private static ConversationState _conversationState;
        private IBotTelemetryClient _telemetryClient;
        private string _traceName = "Feedback";

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

            var allActions = new List<CardAction>(_options.FeedbackActions(context, tag))
            {
                _options.DismissAction(context, tag),
            };

            // If channel supports suggested actions
            if (Channel.SupportsSuggestedActions(context.Activity.ChannelId))
            {
                // prompt for feedback
                // if activity already had suggested actions, add the feedback actions
                if (context.Activity.SuggestedActions != null)
                {
                    var actions = new List<CardAction>()
                        .Concat(context.Activity.SuggestedActions.Actions)
                        .Concat(allActions)
                        .ToList();

                    await context.SendActivityAsync(MessageFactory.SuggestedActions(actions)).ConfigureAwait(false);
                }
                else
                {
                    var actions = allActions;
                    await context.SendActivityAsync(MessageFactory.SuggestedActions(actions)).ConfigureAwait(false);
                }
            }
            else
            {
                // else channel doesn't support suggested actions, so use hero card.
                var hero = new HeroCard(buttons: allActions);
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
                // we don't have feedback
                if (record.Feedback == null)
                {
                    var feedbackActions = _options.FeedbackActions(context, record.Tag);
                    var dismissAction = _options.DismissAction(context, record.Tag);

                    var feedback = feedbackActions.FirstOrDefault(f => context.Activity.Text == (string)f.Value || context.Activity.Text == f.Title);

                    if (feedback != null)
                    {
                        // Set the feedback to the action value for consistency
                        record.Feedback = feedback;
                        await _feedbackAccessor.SetAsync(context, record).ConfigureAwait(false);

                        (string message, bool enableComments) = _options.FeedbackReceivedMessage(context, record.Tag, feedback);
                        if (enableComments)
                        {
                            // if comments are enabled
                            // create comment prompt with dismiss action
                            if (Channel.SupportsSuggestedActions(context.Activity.ChannelId))
                            {
                                var commentPrompt = MessageFactory.SuggestedActions(
                                    text: message,
                                    cardActions: new List<CardAction>() { dismissAction });

                                // prompt for comment
                                await context.SendActivityAsync(commentPrompt).ConfigureAwait(false);
                            }
                            else
                            {
                                // channel doesn't support suggestedActions, so use hero card.
                                var hero = new HeroCard(
                                    text: message,
                                    buttons: new List<CardAction> { dismissAction });

                                // prompt for comment
                                await context.SendActivityAsync(MessageFactory.Attachment(hero.ToAttachment())).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            // comments not enabled, respond and cleanup
                            // send feedback response
                            await context.SendActivityAsync(message).ConfigureAwait(false);

                            // log feedback in appInsights
                            LogFeedback(record);

                            // clear state
                            await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                        }
                    }
                    else if (context.Activity.Text == (string)dismissAction.Value || context.Activity.Text == dismissAction.Title)
                    {
                        // clear state
                        await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                    }
                    else if (_options.CommentsEnabled)
                    {
                        await HandleCommentAsync(context, record).ConfigureAwait(false);
                    }
                    else
                    {
                        // we requested feedback, but the user responded with something else
                        // clear state and continue (so message can be handled by dialog stack)
                        await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                        await next(cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    var dismissAction = _options.DismissAction(context, record.Tag);
                    if (context.Activity.Text == (string)dismissAction.Value || context.Activity.Text == dismissAction.Title)
                    {
                        // clear state
                        await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
                    }
                    else
                    {
                        await HandleCommentAsync(context, record).ConfigureAwait(false);
                    }
                }

                await _conversationState.SaveChangesAsync(context).ConfigureAwait(false);
            }
            else
            {
                // We are not requesting feedback. Go to next.
                await next(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleCommentAsync(ITurnContext context, FeedbackRecord record)
        {
            // store comment in state
            record.Comment = context.Activity.Text;
            await _feedbackAccessor.SetAsync(context, record).ConfigureAwait(false);

            // Respond to comment
            await context.SendActivityAsync(_options.CommentReceivedMessage(context, record.Tag, record.Feedback, record.Comment)).ConfigureAwait(false);

            // log feedback in appInsights
            LogFeedback(record);

            // clear state
            await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
        }

        private void LogFeedback(FeedbackRecord record)
        {
            var properties = new Dictionary<string, string>()
            {
                { nameof(FeedbackRecord.Tag), record.Tag },
                { nameof(FeedbackRecord.Feedback), (string)record.Feedback?.Value },
                { nameof(FeedbackRecord.Comment), record.Comment },
                { nameof(FeedbackRecord.Request.Text), record.Request?.Text },
                { nameof(FeedbackRecord.Request.Id), record.Request?.Conversation.Id },
                { nameof(FeedbackRecord.Request.ChannelId), record.Request?.ChannelId },
            };

            _telemetryClient.TrackEvent(_traceName, properties);
        }
    }
}
