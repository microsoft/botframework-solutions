// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Feedback
{
    /// <summary>
    /// Configures the FeedbackMiddleware object.
    /// </summary>
    [ExcludeFromCodeCoverageAttribute]
    public class FeedbackOptions
    {
        private List<CardAction> feedbackActions;
        private CardAction dismissAction;
        private string feedbackReceivedMessage;
        private string commentPrompt;
        private string commentReceivedMessage;

        /// <summary>
        /// Gets or sets custom feedback choices for the user.
        /// Default values are "👍" and "👎".
        /// </summary>
        /// <value>
        /// Custom feedback choices for the user.
        /// </value>
        public List<CardAction> FeedbackActions
        {
            get
            {
                if (this.feedbackActions == null)
                {
                    return new List<CardAction>()
                    {
                        new CardAction(ActionTypes.PostBack, title: "👍", value: "positive"),
                        new CardAction(ActionTypes.PostBack, title: "👎", value: "negative"),
                    };
                }

                return this.feedbackActions;
            }
            set => this.feedbackActions = value;
        }

        /// <summary>
        /// Gets or sets text to show on button that allows user to hide/ignore the feedback request.
        /// </summary>
        /// <value>
        /// Text to show on button that allows user to hide/ignore the feedback request.
        /// </value>
        public CardAction DismissAction
        {
            get
            {
                if (this.dismissAction == null)
                {
                    return new CardAction(ActionTypes.PostBack, title: FeedbackResponses.DismissTitle, value: "dismiss");
                }

                return this.dismissAction;
            }
            set => this.dismissAction = value;
        }

        /// <summary>
        /// Gets or sets message to show when a user provides some feedback.
        /// Default value is "Thanks, I appreciate your feedback.".
        /// </summary>
        /// <value>
        /// Message to show when a user provides some feedback.
        /// </value>
        public string FeedbackReceivedMessage
        {
            get
            {
                if (string.IsNullOrEmpty(this.feedbackReceivedMessage))
                {
                    return FeedbackResponses.FeedbackReceivedMessage;
                }

                return this.feedbackReceivedMessage;
            }
            set => this.feedbackReceivedMessage = value;
        }

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
        public string CommentPrompt
        {
            get
            {
                if (string.IsNullOrEmpty(this.commentPrompt))
                {
                    return FeedbackResponses.CommentPrompt;
                }

                return this.commentPrompt;
            }
            set => this.commentPrompt = value;
        }

        /// <summary>
        /// Gets or sets the message to show when a user's comment has been received.
        /// Default value is "Your comment has been received.".
        /// </summary>
        /// <value>
        /// The message to show when a user's comment has been received.
        /// </value>
        public string CommentReceivedMessage
        {
            get
            {
                if (string.IsNullOrEmpty(this.commentReceivedMessage))
                {
                    return FeedbackResponses.CommentReceivedMessage;
                }

                return this.commentReceivedMessage;
            }
            set => this.commentReceivedMessage = value;
        }
    }
}
