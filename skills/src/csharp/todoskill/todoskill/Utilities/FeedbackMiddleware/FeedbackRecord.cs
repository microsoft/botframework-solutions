using Microsoft.Bot.Schema;

namespace ToDoSkill.Utilities.FeedbackMiddleware
{
    public class FeedbackRecord
    {
        public string Tag { get; set; }

        // activity sent by the user that triggered the feedback request
        public Activity Request { get; set; }

        // bot text or value for which feedback is being requested
        public string Response { get; set; }

        // user's feedback selection
        public string Feedback { get; set; }

        // user's free-form comments, if enabled
        public string Comments { get; set; }
    }
}
