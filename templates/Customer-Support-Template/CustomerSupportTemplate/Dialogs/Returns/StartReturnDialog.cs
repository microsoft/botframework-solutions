using CustomerSupportTemplate.Dialogs.Shared;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Returns
{
    public class StartReturnDialog : CustomerSupportDialog
    {
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private ReturnResponses _responder = new ReturnResponses();

        public StartReturnDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor,
            IBotTelemetryClient telemetryClient)
            : base(services, nameof(StartReturnDialog), telemetryClient)
        {
            _services = services;
            _stateAccessor = stateAccessor;
            TelemetryClient = TelemetryClient;

            var startReturn = new WaterfallStep[]
            {
                ShowPolicy,
                PromptToEscalate,
                HandleEscalationResponse,
            };

            InitialDialogId = nameof(StartReturnDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, startReturn) { TelemetryClient = telemetryClient });
            AddDialog(new ConfirmPrompt(DialogIds.EscalatePrompt, SharedValidators.ConfirmValidator));
        }

        private async Task<DialogTurnResult> ShowPolicy(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // show policy
            await _responder.ReplyWith(stepContext.Context, ReturnResponses.ResponseIds.ReturnPolicyCard);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptToEscalate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.EscalatePrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ReturnResponses.ResponseIds.StartReturnPrompt),
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
