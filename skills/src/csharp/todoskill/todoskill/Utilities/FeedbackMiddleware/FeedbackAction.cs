using Microsoft.Bot.Schema;

namespace ToDoSkill.Utilities.FeedbackMiddleware
{
    public class FeedbackAction
    {
        public string Text { get; set; } = null;

        public CardAction CardAction { get; set; } = null;
    }
}
