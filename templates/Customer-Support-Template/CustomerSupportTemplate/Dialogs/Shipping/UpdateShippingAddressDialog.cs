using CustomerSupportTemplate.Dialogs.Shared;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Shipping
{
    public class UpdateShippingAddressDialog : CustomerSupportDialog
    {
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private ShippingResponses _responder = new ShippingResponses();

        public UpdateShippingAddressDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor,
            IBotTelemetryClient telemetryClient)
            : base(services, nameof(UpdateShippingAddressDialog), telemetryClient)
        {
            _services = services;
            _stateAccessor = stateAccessor;
            TelemetryClient = telemetryClient;

            var updateShippingAddress = new WaterfallStep[]
            {
                ShowPolicy,
                PromptToEscalate,
                HandleEscalationResponse,
            };

            InitialDialogId = nameof(UpdateShippingAddressDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, updateShippingAddress) { TelemetryClient = telemetryClient });
            AddDialog(new ConfirmPrompt(DialogIds.EscalatePrompt, SharedValidators.ConfirmValidator));
        }

        private async Task<DialogTurnResult> ShowPolicy(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, ShippingResponses.ResponseIds.UpdateAddressPolicyMessage);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptToEscalate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.EscalatePrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ShippingResponses.ResponseIds.FindAgentPrompt)
            });
        }

        private async Task<DialogTurnResult> HandleEscalationResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (bool)stepContext.Result;

            if (result)
            {
                return await stepContext.BeginDialogAsync(nameof(EscalateDialog));
            }
            else
            {
                return await stepContext.EndDialogAsync();
            }
        }

        private class DialogIds
        {
            public const string EscalatePrompt = "escalatePrompt";
        }
    }
}
