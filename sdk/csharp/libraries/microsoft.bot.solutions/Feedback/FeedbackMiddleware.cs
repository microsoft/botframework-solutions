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
        private FeedbackOptions _options;
        private IStatePropertyAccessor<FeedbackRecord> _feedbackAccessor;
        private ConversationState _conversationState;
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
            var middleware = context.TurnState.Get<FeedbackMiddleware>();

            // clear state
            await middleware._feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);

            // create feedbackRecord with original activity and tag
            var record = new FeedbackRecord()
            {
                Request = context.Activity,
                Tag = tag,
            };

            // store in state. No need to save changes, because its handled in IBot
            await middleware._feedbackAccessor.SetAsync(context, record).ConfigureAwait(false);

            var allActions = new List<CardAction>(middleware._options.FeedbackActions(context, tag))
            {
                middleware._options.DismissAction(context, tag),
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
            context.TurnState.Add(this);

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

                        (var message, bool enableComments) = _options.FeedbackReceivedMessage(context, record.Tag, feedback);
                        if (enableComments)
                        {
                            // if comments are enabled
                            // create comment prompt with dismiss action
                            if (Channel.SupportsSuggestedActions(context.Activity.ChannelId))
                            {
                                if (message.SuggestedActions.Actions == null)
                                {
                                    message.SuggestedActions.Actions = new List<CardAction>() { dismissAction };
                                }
                                else
                                {
                                    message.SuggestedActions.Actions.Add(dismissAction);
                                }

                                // prompt for comment
                                await context.SendActivityAsync(message).ConfigureAwait(false);
                            }
                            else
                            {
                                // channel doesn't support suggestedActions, so use hero card.
                                var hero = new HeroCard(buttons: new List<CardAction> { dismissAction }).ToAttachment();
                                if (message.Attachments == null)
                                {
                                    message.Attachments = new List<Attachment>() { hero };
                                }
                                else
                                {
                                    message.Attachments.Add(hero);
                                }

                                // prompt for comment
                                await context.SendActivityAsync(message).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            // comments not enabled, respond and cleanup
                            // send feedback response
                            if (message != null)
                            {
                                await context.SendActivityAsync(message).ConfigureAwait(false);
                            }

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
            var message = _options.CommentReceivedMessage(context, record.Tag, record.Feedback, record.Comment);
            if (message != null)
            {
                await context.SendActivityAsync(message).ConfigureAwait(false);
            }

            // log feedback in appInsights
            LogFeedback(record);

            // clear state
            await _feedbackAccessor.DeleteAsync(context).ConfigureAwait(false);
        }

        private void LogFeedback(FeedbackRecord record)
        {
            var properties = new Dictionary<string, string>()
            {
                { FeedbackConstants.FeedbackTag, record.Tag },
                { FeedbackConstants.FeedbackValue, (string)record.Feedback?.Value },
                { FeedbackConstants.FeedbackComent, record.Comment },
                { TelemetryConstants.ConversationIdProperty, record.Request?.Conversation.Id },
                { TelemetryConstants.ChannelIdProperty, record.Request?.ChannelId },
            };

            if (_options.LogPersonalInformation)
            {
                properties.Add(TelemetryConstants.TextProperty, record.Request?.Text);
            }

            _telemetryClient.TrackEvent(FeedbackConstants.FeedbackEvent, properties);
        }
    }
}
