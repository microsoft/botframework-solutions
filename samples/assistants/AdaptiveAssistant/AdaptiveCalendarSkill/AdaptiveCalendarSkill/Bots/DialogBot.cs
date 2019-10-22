using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdaptiveCalendarSkill.Bots
{
    public class DialogBot<T> : ActivityHandler
        where T : Dialog
    {
        private DialogManager _dialogManager;

        public DialogBot(T dialog, IConfiguration configuration, ConversationState conversationState)
        {
            _dialogManager = new DialogManager(dialog);
        }

        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.OnTurnAsync(turnContext, cancellationToken);
        }

        protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
        }

        protected override Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Event activity received {turnContext.Activity.Name}"));
            return _dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
        }

        protected override Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialogManager.OnTurnAsync(turnContext, cancellationToken: cancellationToken);
        }
    }
}
