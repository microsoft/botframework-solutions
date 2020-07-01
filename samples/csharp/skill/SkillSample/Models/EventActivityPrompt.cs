using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace SkillSample.Models
{
    /// <summary>
    /// EventActivityPrompt enables user to add a prompt to the stack that will optionally except Event Activities.
    /// </summary>
    public class EventActivityPrompt : ActivityPrompt
    {

        public EventActivityPrompt(string dialogId, PromptValidator<Activity> validator)
        : base(dialogId, validator)
        {
        }

        public async Task OnPromptNullContext(object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var opt = (PromptOptions)options;

            // should throw ArgumentNullException
            await OnPromptAsync(turnContext: null, state: null, options: opt, isRetry: false);
        }

        public async Task OnPromptNullOptions(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // should throw ArgumentNullException
            await OnPromptAsync(dc.Context, state: null, options: null, isRetry: false);
        }
    }
}
