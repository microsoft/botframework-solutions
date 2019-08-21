using System.Collections.Generic;
using Microsoft.Bot.Builder;

namespace ToDoSkill.Utilities.FeedbackMiddleware
{
    public class FeedbackOptions
    {
        // Custom feedback choices for the user. Default values are: "👍 good answer", "👎 bad answer"
        public List<FeedbackAction> FeedbackActions { get; set; }

        // Optionally enable prompting for free-form comments for all or select feedback choices (free-form prompt is shown after user selects a preset choice)
        public PromptFreeForm PromptFreeForm { get; set; }

        // Message to show when a user provides some feedback. Default value is `'Thanks for your feedback!'`
        public string FeedbackResponse { get; set; }

        // Text to show on button that allows user to hide/ignore the feedback request. Default value is `'dismiss'`
        public FeedbackAction DismissAction { get; set; }

        // Message to show when `promptFreeForm` is enabled. Default value is `'Please add any additional comments in the chat'`
        public string FreeFormPrompt { get; set; }

        public ConversationState ConversationState { get; set; }
    }
}
