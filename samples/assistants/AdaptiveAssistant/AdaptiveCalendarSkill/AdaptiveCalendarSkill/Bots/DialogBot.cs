using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveCalendarSkill.Bots
{
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        private DialogManager _dialogManager;

        public DialogBot(T dialog, ConversationState conversationState)
        {
            _dialogManager = new DialogManager(dialog);
        }

        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
        }
    }
}
