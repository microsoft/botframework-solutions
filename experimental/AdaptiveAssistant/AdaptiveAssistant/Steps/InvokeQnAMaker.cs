using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveAssistant.Steps
{
    public class InvokeQnAMaker : DialogCommand
    {
        private QnAMaker _qnaService;

        public InvokeQnAMaker(QnAMaker qnaService)
        {
            _qnaService = qnaService;
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var answers = await _qnaService.GetAnswersAsync(dc.Context);

            if (answers != null && answers.Count() > 0)
            {
                await dc.Context.SendActivityAsync(answers[0].Answer, speak: answers[0].Answer);
            }
            else
            {
                // AdaptiveEvents.UnknownIntent
                await dc.EmitEventAsync("NoQnAMatch", cancellationToken);
            }

            return await dc.EndDialogAsync();
        }
    }
}
