using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Prompts
{
    public class EventPrompt : ActivityPrompt
    {
        public EventPrompt(string dialogId, string eventName, PromptValidator<Activity> validator)
            : base(dialogId, validator)
        {
            EventName = eventName;
        }

        public string EventName { get; set; }

        protected override Task<PromptRecognizerResult<Activity>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken)
        {
            var result = new PromptRecognizerResult<Activity>();
            var activity = turnContext.Activity;

            if (activity.Type == ActivityTypes.Event)
            {
                var ev = activity.AsEventActivity();

                if (ev.Name == EventName)
                {
                    result.Succeeded = true;
                    result.Value = turnContext.Activity;
                }
            }

            return Task.FromResult(result);
        }
    }
}