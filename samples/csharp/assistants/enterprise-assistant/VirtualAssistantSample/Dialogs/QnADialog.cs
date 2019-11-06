using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Schema;

namespace VirtualAssistantSample.Dialogs
{
    public class QnADialog : Dialog
    {
        private QnAMaker _qnaMaker;

        public QnADialog(QnAMaker qnaMaker)
            : base(nameof(QnADialog))
        {
            _qnaMaker = qnaMaker;
        }

        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext dialogContext, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ContinueDialogAsync(dialogContext, cancellationToken);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dialogContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var inputActivity = dialogContext.Context.Activity;
            IActivity outputActivity = new Activity();
            var query = inputActivity.Text;
            var qnaResult = await _qnaMaker.GetAnswersAsync(dialogContext.Context);
            if (qnaResult.Any())
            {
                var qnaAnswer = qnaResult[0].Answer;
                var prompts = qnaResult[0].Context?.Prompts;

                if (prompts == null || prompts.Length < 1)
                {
                    outputActivity = MessageFactory.Text(qnaAnswer);
                }
                else
                {
                    outputActivity = MessageFactory.SuggestedActions(text: qnaAnswer, actions: prompts.Select(p => p.DisplayText));
                }

                await dialogContext.Context.SendActivityAsync(outputActivity);
                return EndOfTurn;
            }

            dialogContext.SuppressCompletionMessage(true);

            await dialogContext.EndDialogAsync();

            return await dialogContext.ContinueDialogAsync();
        }
    }
}