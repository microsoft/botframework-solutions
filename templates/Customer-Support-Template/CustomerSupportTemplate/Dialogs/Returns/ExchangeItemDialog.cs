using CustomerSupportTemplate.Dialogs.Returns.Resources;
using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Returns
{
    public class ExchangeItemDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private ReturnResponses _responder = new ReturnResponses();

        public ExchangeItemDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(ExchangeItemDialog))
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;

            var exchangeItem = new WaterfallStep[]
            {
                ShowPolicy,
                PromptForExchangeType,
                HandleExchangeTypeResponse,
            };

            var showStores = new WaterfallStep[]
            {
                PromptForZipCode,
                ShowNearbyStores,
            };

            InitialDialogId = nameof(ExchangeItemDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, exchangeItem));
            AddDialog(new WaterfallDialog(DialogIds.GetNearbyStoresDialog, showStores));
            AddDialog(new ChoicePrompt(DialogIds.ExchangeTypePrompt, SharedValidators.ChoiceValidator));
            AddDialog(new TextPrompt(DialogIds.ZipCodePrompt, SharedValidators.ZipCodeValidator));
        }

        private async Task<DialogTurnResult> ShowPolicy(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, ReturnResponses.ResponseIds.ExchangePolicyCard);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptForExchangeType(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.ExchangeTypePrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ReturnResponses.ResponseIds.ExchangeTypePrompt),
                Choices = new List<Choice>()
                {
                    new Choice()
                    {
                        Value = ReturnStrings.ExchangeTypeStore,
                        Action = new CardAction(ActionTypes.ImBack, title: ReturnStrings.ExchangeTypeStore, value: ReturnStrings.ExchangeTypeStore),
                    },
                    new Choice()
                    {
                        Value = ReturnStrings.ExchangeTypeAgent,
                        Action = new CardAction(ActionTypes.ImBack, title: ReturnStrings.ExchangeTypeAgent, value: ReturnStrings.ExchangeTypeAgent),
                    },
                }
            });
        }

        private async Task<DialogTurnResult> HandleExchangeTypeResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = stepContext.Result as FoundChoice;
            if (choice.Index == 0)
            {
                return await stepContext.BeginDialogAsync(DialogIds.GetNearbyStoresDialog);
            }
            else if (choice.Index == 1)
            {
                return await stepContext.BeginDialogAsync(nameof(EscalateDialog));
            }

            return await stepContext.EndDialogAsync();
        }

        private async Task<DialogTurnResult> PromptForZipCode(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.ZipCodePrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ReturnResponses.ResponseIds.ZipCodePrompt),
            });
        }

        private async Task<DialogTurnResult> ShowNearbyStores(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var zip = (string)stepContext.Result;

            // lookup zip in store list
            var stores = _client.GetStoresByZipCode(zip);

            // send response with 3 nearest stores
            await _responder.ReplyWith(stepContext.Context, ReturnResponses.ResponseIds.NearbyStores, stores);

            return await stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string ExchangeTypePrompt = "exchangeTypePrompt";
            public const string ZipCodePrompt = "zipCodePrompt";
            public static string GetNearbyStoresDialog = "getNearbyStoresDialog";
        }
    }
}
