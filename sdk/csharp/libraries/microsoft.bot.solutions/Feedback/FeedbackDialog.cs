using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Feedback
{
    public class FeedbackDialog : ComponentDialog
    {
        private static ConversationState ConversationState;
        private static FeedbackOptions FeedbackOptions;
        private static string FeedbackTag;
        private static IStatePropertyAccessor<FeedbackRecord> FeedbackAccessor;
        private static string TraceName = "Feedback";
        private static IBotTelemetryClient TelemetryClient;

        public FeedbackDialog(
            ConversationState conversationState,
            FeedbackOptions feedbackOptions,
            IBotTelemetryClient telemetryClient
            )
        : base(nameof(FeedbackDialog))
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            FeedbackOptions = feedbackOptions ?? throw new ArgumentNullException(nameof(feedbackOptions));
            TelemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));

            // Create FeedbackRecord state accessor
            FeedbackAccessor = conversationState.CreateProperty<FeedbackRecord>(nameof(FeedbackRecord));

            InitialDialogId = nameof(WaterfallDialog);

            var steps = new WaterfallStep[]
            {
                RequestFeedback,
                RequestFeedbackComment,
                EndFeedbackDialog,
            };

            // Add named dialogs to dialog set
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), steps));
            AddDialog(new TextPrompt(DialogIds.RequestFeedbackPrompt));
            AddDialog(new TextPrompt(DialogIds.RequestFeedbackCommentPrompt));
        }

        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext outerDc, object options = null, CancellationToken cancellationToken = default)
        {
            FeedbackTag = (options is string && !String.IsNullOrEmpty(options.ToString())) ? options.ToString() : throw new ArgumentException("Feedback tag sent to FeedbackDialog is not a string or is null");
            return base.BeginDialogAsync(outerDc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            // continue parent dialog
            await outerDc.EndDialogAsync().ConfigureAwait(false);
            return await outerDc.Parent.ContinueDialogAsync().ConfigureAwait(false);
        }

        private static async Task<DialogTurnResult> RequestFeedback(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // clear state
            await FeedbackAccessor.DeleteAsync(stepContext.Context).ConfigureAwait(false);

            // create feedbackRecord with original activity and tag
            var record = new FeedbackRecord()
            {
                Request = stepContext.Context.Activity,
                Tag = FeedbackTag,
            };

            // store in state. No need to save changes, because its handled in IBot
            await FeedbackAccessor.SetAsync(stepContext.Context, record).ConfigureAwait(false);

            Activity requestFeedbackActivity;

            if (Channel.SupportsSuggestedActions(stepContext.Context.Activity.ChannelId))
            {
                requestFeedbackActivity = (Activity)MessageFactory.SuggestedActions(GetFeedbackActions());
            } else
            {
                requestFeedbackActivity = (Activity)MessageFactory.Attachment(new HeroCard(buttons: GetFeedbackActions()).ToAttachment());
            }

            return await stepContext.PromptAsync(DialogIds.RequestFeedbackPrompt, new PromptOptions {Prompt = requestFeedbackActivity }, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<DialogTurnResult> RequestFeedbackComment(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var record = await FeedbackAccessor.GetAsync(stepContext.Context, () => null).ConfigureAwait(false);

            string feedback;
            if (UserGaveFeedback(stepContext.Result.ToString(), out feedback))
            {
                // If user selected one of the feedback options
                record.Feedback = feedback;
                await FeedbackAccessor.SetAsync(stepContext.Context, record).ConfigureAwait(false);

                if (FeedbackOptions.CommentsEnabled)
                {
                    var commentPrompt = MessageFactory.SuggestedActions(
                        text: $"{FeedbackOptions.FeedbackReceivedMessage} {FeedbackOptions.CommentPrompt}",
                        cardActions: new List<CardAction>() { FeedbackOptions.DismissAction });
                    return await stepContext.PromptAsync(DialogIds.RequestFeedbackCommentPrompt, new PromptOptions { Prompt = (Activity)commentPrompt }, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // comments not enabled
                    return await stepContext.NextAsync().ConfigureAwait(false);
                }
            }
            else
            {
                // If users query did not match a feeback option
                // End dialog and continue parent dialog with users query
                return await stepContext.EndDialogAsync().ConfigureAwait(false);
            }
        }

        private static async Task<DialogTurnResult> EndFeedbackDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var record = await FeedbackAccessor.GetAsync(stepContext.Context, () => null).ConfigureAwait(false);

            if (FeedbackOptions.CommentsEnabled && stepContext.Result is string && !string.IsNullOrEmpty(stepContext.Result.ToString()))
            {
                // user responded with some string
                if (stepContext.Result.ToString() != FeedbackOptions.DismissAction.Value.ToString() && stepContext.Result.ToString() != FeedbackOptions.DismissAction.Value.ToString())
                {
                    // user did not dismiss the comment prompt so the string sent was feedback comment
                    record.Comment = stepContext.Result.ToString();
                }
            }

            // send feedback response
            await stepContext.Context.SendActivityAsync(FeedbackOptions.FeedbackReceivedMessage).ConfigureAwait(false);

            // log feedback in appInsights
            LogFeedback(record);

            // clear state
            await FeedbackAccessor.DeleteAsync(stepContext.Context).ConfigureAwait(false);

            return await stepContext.EndDialogAsync().ConfigureAwait(false);
        }

        private static List<CardAction> GetFeedbackActions()
        {
            var actions = new List<CardAction>(FeedbackOptions.FeedbackActions)
            {
                FeedbackOptions.DismissAction,
            };
            return actions;
        }

        private static bool UserGaveFeedback(string result, out string feedback)
        {
            foreach (var action in GetFeedbackActions())
            {
                if (action.Value != null && action.Value.ToString() == result)
                {
                    feedback = result;
                    return true;
                }
            }
            feedback = null;
            return false;
        }

        private static void LogFeedback(FeedbackRecord record)
        {
            var properties = new Dictionary<string, string>()
            {
                { nameof(FeedbackRecord.Tag), record.Tag },
                { nameof(FeedbackRecord.Feedback), record.Feedback },
                { nameof(FeedbackRecord.Comment), record.Comment },
                { nameof(FeedbackRecord.Request.Text), record.Request?.Text },
                { nameof(FeedbackRecord.Request.Id), record.Request?.Conversation.Id },
                { nameof(FeedbackRecord.Request.ChannelId), record.Request?.ChannelId },
            };
            TelemetryClient.TrackEvent(TraceName, properties);
        }

        private class DialogIds
        {
            public const string RequestFeedbackPrompt = "RequestFeedbackPrompt";
            public const string RequestFeedbackCommentPrompt = "RequestFeedbackCommentPrompt ";
        }
    }
}
