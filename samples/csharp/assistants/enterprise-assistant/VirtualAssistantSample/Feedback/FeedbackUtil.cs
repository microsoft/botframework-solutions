using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace VirtualAssistantSample.Feedback
{
    public class FeedbackUtil
    {
        /// <summary>
        /// Creates a 'prompt for feedback' activity with message text and feedback actions defined by passed FeedbackOptions param.
        /// </summary>
        /// <param name="context">ITurnContext used to extract channel info to ensure feedback actions are renderable in current channel.</param>
        /// <param name="feedbackOptions">FeedbackOptions instance used to customize feedback experience.</param>
        /// <returns>
        /// A 'prompt for feedback' Activity.
        /// Has feedback options displayed in a manner supported by the current channel.
        /// </returns>
        public static Activity CreateFeedbackActivity(ITurnContext context, FeedbackOptions feedbackOptions = null)
        {
            // Check for null options param, if null, instanticate default
            feedbackOptions = feedbackOptions == null ? new FeedbackOptions() : feedbackOptions;
            var feedbackActivity = ChoiceFactory.ForChannel(context.Activity.ChannelId, new List<Choice>(feedbackOptions.FeedbackActions) { feedbackOptions.DismissAction }, feedbackOptions.FeedbackPromptMessage);
            return (Activity)feedbackActivity;
        }

        /// <summary>
        /// Creates a 'prompt for feedback comment' activity with message text and dismess action defined by passed FeedbackOptions param.
        /// </summary>
        /// <param name="context">ITurnContext used to extract channel info to ensure dismiss action is renderable in current channel.</param>
        /// <param name="feedbackOptions">FeedbackOptions instance used to customize comment prompt text and dismiss action.</param>
        /// <returns>
        /// A 'prompt for feedback comment' Activity.
        /// Has dismiss option displayed in a manner supported by the current channel.
        /// </returns>
        public static Activity GetFeedbackCommentPrompt(ITurnContext context, FeedbackOptions feedbackOptions = null)
        {
            // Check for null options param, if null, instanticate default
            feedbackOptions = feedbackOptions == null ? new FeedbackOptions() : feedbackOptions;
            var message = ChoiceFactory.ForChannel(context.Activity.ChannelId, new List<Choice>() { feedbackOptions.DismissAction }, $"{feedbackOptions.FeedbackReceivedMessage} {feedbackOptions.CommentPrompt}");

            return (Activity)message;
        }

        /// <summary>
        /// Sends feedback to be logged.
        /// </summary>
        /// <param name="record">FeedbackRecord object to be logged.</param>
        /// <param name="telemetryClient">IBotTelemetryClient object used to log feedback record<param>
        public static void LogFeedback(FeedbackRecord record, IBotTelemetryClient telemetryClient)
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

            telemetryClient.TrackEvent("Feedback", properties);
        }

        /// <summary>
        /// Object used when user responds to a feedback prompt with a query intended to be handled by main bot logic.
        /// When the feedback prompt detects the above condition it sends this object to next feedback steps so the users query skips feedback and is routed to appropriate bot logic.
        /// </summary>
        public class RouteQueryFlag
        {
            public bool RouteQuery { get; set; }
        }
    }
}
