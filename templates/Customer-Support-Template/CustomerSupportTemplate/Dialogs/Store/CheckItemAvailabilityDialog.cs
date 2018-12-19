using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Store
{
    public class CheckItemAvailabilityDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private StoreResponses _responder = new StoreResponses();

        public CheckItemAvailabilityDialog(
            BotServices services,
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor,
            IBotTelemetryClient telemetryClient)
            : base(services, nameof(CheckItemAvailabilityDialog), telemetryClient)
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;
            TelemetryClient = telemetryClient;

            var checkAvailability = new WaterfallStep[]
            {
                PromptForItemNumber,
                PromptForZipCode,
                ShowStores,
                PromptToHoldItem,
                HandleHoldItemResponse,
            };

            InitialDialogId = nameof(CheckItemAvailabilityDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, checkAvailability) { TelemetryClient = telemetryClient });
            AddDialog(new TextPrompt(DialogIds.ItemNumberPrompt, SharedValidators.ItemNumberValidator));
            AddDialog(new TextPrompt(DialogIds.ZipCodePrompt, SharedValidators.ZipCodeValidator));
            AddDialog(new ConfirmPrompt(DialogIds.HoldItemPrompt, SharedValidators.ConfirmValidator));
        }

        private async Task<DialogTurnResult> PromptForItemNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.ItemNumberPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, StoreResponses.ResponseIds.ItemIdPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, StoreResponses.ResponseIds.ItemIdReprompt),
            });
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
            // get zip code from result
            var zip = string.Empty;
            var itemId = string.Empty;

            var stores = _client.GetStoresWithItemByZip(zip, itemId);

            await _responder.ReplyWith(stepContext.Context, StoreResponses.ResponseIds.NearbyStoresMessage);
            await _responder.ReplyWith(stepContext.Context, StoreResponses.ResponseIds.StoresWithProductCard, stores);

            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptToHoldItem(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.HoldItemPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, StoreResponses.ResponseIds.HoldItemPrompt),
            });
        }

        private async Task<DialogTurnResult> HandleHoldItemResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (bool)stepContext.Result;

            if (result)
            {
                var storeId = string.Empty;
                var itemId = string.Empty;
                var accountId = string.Empty;

                // place the item on hold
                _client.HoldItem(storeId, itemId, accountId);

                await _responder.ReplyWith(stepContext.Context, StoreResponses.ResponseIds.HoldItemSuccessMessage);
            }

            return await stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string ItemNumberPrompt = "itemNumberPrompt";
            public const string ZipCodePrompt = "zipCodePrompt";
            public const string HoldItemPrompt = "holdItemPrompt";
        }
    }
}
