// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Feedback
{
    /// <summary>
    /// Configures the FeedbackMiddleware object.
    /// </summary>
    public class FeedbackOptions
    {
        public delegate IEnumerable<CardAction> FeedbackActionsDelegate(ITurnContext context, string tag);

        public delegate CardAction DismissActionDelegate(ITurnContext context, string tag);

        public delegate (string, bool) FeedbackReceivedMessageDelegate(ITurnContext context, string tag, CardAction action);

        public delegate string CommentReceivedMessageDelegate(ITurnContext context, string tag, CardAction action, string comment);

        /// <summary>
        /// Gets or sets custom feedback choices for the user.
        /// Default values are "👍" and "👎".
        /// </summary>
        /// <value>
        /// Custom feedback choices for the user.
        /// </value>
        public FeedbackActionsDelegate FeedbackActions { get; set; } = (ITurnContext context, string tag) =>
        {
            return new List<CardAction>()
                {
                    new CardAction(ActionTypes.PostBack, title: "👍", value: "positive"),
                    new CardAction(ActionTypes.PostBack, title: "👎", value: "negative"),
                };
        };

        /// <summary>
        /// Gets or sets text to show on button that allows user to hide/ignore the feedback request.
        /// </summary>
        /// <value>
        /// Text to show on button that allows user to hide/ignore the feedback request.
        /// </value>
        public DismissActionDelegate DismissAction { get; set; } = (ITurnContext context, string tag) =>
        {
            return new CardAction(ActionTypes.PostBack, title: FeedbackResponses.DismissTitle, value: "dismiss");
        };

        /// <summary>
        /// Gets or sets message to show and wether to prompt for comments when a user provides some feedback.
        /// Default value is "Thanks, I appreciate your feedback." when positive and "Thanks, I appreciate your feedback. Please add any additional comments in the chat." when negative.
        /// </summary>
        /// <value>
        /// Message to show and wether to prompt for comments when a user provides some feedback.
        /// </value>
        public FeedbackReceivedMessageDelegate FeedbackReceivedMessage { get; set; } = (ITurnContext context, string tag, CardAction action) =>
        {
            if ((string)action.Value == "negative")
            {
                return ($"{FeedbackResponses.FeedbackReceivedMessage} {FeedbackResponses.CommentPrompt}", true);
            }
            else
            {
                return (FeedbackResponses.FeedbackReceivedMessage, false);
            }
        };

        /// <summary>
        /// Gets or sets a value indicating whether treat message as comment after user doesn't select a preset choice.
        /// Default value is false.
        /// </summary>
        /// <value>
        /// A value indicating whether treat message as comment after user doesn't select a preset choice.
        /// </value>
        public bool CommentsEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the message to show when a user's comment has been received.
        /// Default value is "Your comment has been received.".
        /// </summary>
        /// <value>
        /// The message to show when a user's comment has been received.
        /// </value>
        public CommentReceivedMessageDelegate CommentReceivedMessage { get; set; } = (ITurnContext context, string tag, CardAction action, string comment) =>
         {
             return FeedbackResponses.CommentReceivedMessage;
         };
    }
}
