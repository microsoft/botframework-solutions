using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Store
{
    public class FindStoreDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private StoreResponses _responder = new StoreResponses();

        public FindStoreDialog(
            BotServices services,
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(FindStoreDialog))
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;

            var findStore = new WaterfallStep[]
            {
                PromptForZipCode,
                ShowStores,
            };

            InitialDialogId = nameof(FindStoreDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, findStore));
            AddDialog(new TextPrompt(DialogIds.ZipCodePrompt, SharedValidators.ZipCodeValidator));
        }

        private async Task<DialogTurnResult> PromptForZipCode(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.ZipCodePrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, StoreResponses.ResponseIds.ZipCodePrompt),
            });
        }

        private async Task<DialogTurnResult> ShowStores(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var zip = string.Empty;

            var stores = _client.GetStoresByZipCode(zip);

            await _responder.ReplyWith(stepContext.Context, StoreResponses.ResponseIds.NearbyStoresMessage);
            await _responder.ReplyWith(stepContext.Context, StoreResponses.ResponseIds.StoresWithProductCard, stores);

            return await stepContext.NextAsync();
        }

        private class DialogIds
        {
            public const string ZipCodePrompt = "zipCodePrompt";
        }
    }
}
