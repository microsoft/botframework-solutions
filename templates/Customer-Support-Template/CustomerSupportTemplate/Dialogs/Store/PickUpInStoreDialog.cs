using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Store
{
    public class PickUpInStoreDialog : CustomerSupportDialog
    {
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private StoreResponses _responder = new StoreResponses();

        public PickUpInStoreDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(PickUpInStoreDialog))
        {
            _services = services;
            _stateAccessor = stateAccessor;

            var pickupInStore = new WaterfallStep[]
            {
                ShowPolicy,
            };

            InitialDialogId = nameof(PickUpInStoreDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, pickupInStore));
        }

        private async Task<DialogTurnResult> ShowPolicy(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, StoreResponses.ResponseIds.PickUpInStoreCard);
            return await stepContext.EndDialogAsync();
        }
    }
}
