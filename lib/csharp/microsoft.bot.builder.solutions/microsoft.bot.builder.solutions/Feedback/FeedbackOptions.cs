using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Feedback
{
    public class FeedbackOptions
    {
        /// <summary>
        /// Gets or sets custom feedback choices for the user.
        /// Default values are "👍" and "👎".
        /// </summary>
        /// <value>
        /// Custom feedback choices for the user.
        /// </value>
        public List<CardAction> FeedbackActions { get; set; } = new List<CardAction>()
        {
            new CardAction(ActionTypes.PostBack, title: "👍", value: "positive"),
            new CardAction(ActionTypes.PostBack, title: "👎", value: "negative"),
        };

        /// <summary>
        /// Gets or sets text to show on button that allows user to hide/ignore the feedback request.
        /// </summary>
        /// <value>
        /// Text to show on button that allows user to hide/ignore the feedback request.
        /// </value>
        public CardAction DismissAction { get; set; } = new CardAction(ActionTypes.PostBack, title: "Dismiss", value: "dismiss");

        /// <summary>
        /// Gets or sets message to show when a user provides some feedback.
        /// Default value is "Thanks for your feedback!".
        /// </summary>
        /// <value>
        /// Message to show when a user provides some feedback.
        /// </value>
        public string HaveFeedbackResponse { get; set; } = "Thanks for your feedback!";

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets flag to prompt for free-form
        /// comments for all or select feedback choices (comment prompt is shown after user selects a preset choice).
        /// Default value is false.
        /// </summary>
        /// <value>
        /// A value indicating whether gets or sets flag to prompt for free-form comments for all or select feedback choices.
        /// </value>
        public bool CommentsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the message to show when `CommentsEnabled` is true.
        /// Default value is "Please add any additional comments in the chat.".
        /// </summary>
        /// <value>
        /// The message to show when `CommentsEnabled` is true.
        /// </value>
        public string CommentPrompt { get; set; } = "Please add any additional comments in the chat.";

        /// <summary>
        /// Gets or sets the message to show when a user's comment has been received.
        /// Default value is "Your comment has been received.".
        /// </summary>
        /// <value>
        /// The message to show when a user's comment has been received.
        /// </value>
        public string CommentReceived { get; set; } = "Your comment has been received.";
    }
}
