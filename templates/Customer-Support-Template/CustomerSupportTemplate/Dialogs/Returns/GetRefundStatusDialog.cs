using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Returns
{
    public class GetRefundStatusDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private ReturnResponses _responder = new ReturnResponses();

        public GetRefundStatusDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(GetRefundStatusDialog))
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;

            var getRefundStatus = new WaterfallStep[]
            {
                PromptForOrderNumber,
                PromptForPhoneNumber,
                ShowRefundStatus
            };

            InitialDialogId = nameof(GetRefundStatusDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, getRefundStatus));
            AddDialog(new TextPrompt(DialogIds.OrderNumberPrompt, SharedValidators.OrderNumberValidator));
            AddDialog(new TextPrompt(DialogIds.PhoneNumberPrompt, SharedValidators.PhoneNumberValidator));
        }

        private async Task<DialogTurnResult> PromptForOrderNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.OrderNumberPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ReturnResponses.ResponseIds.OrderNumberPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ReturnResponses.ResponseIds.OrderNumberReprompt),
            });
        }

        private async Task<DialogTurnResult> PromptForPhoneNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.PhoneNumberPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ReturnResponses.ResponseIds.PhoneNumberPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, ReturnResponses.ResponseIds.PhoneNumberReprompt),
            });
        }

        private async Task<DialogTurnResult> ShowRefundStatus(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var orderNumber = string.Empty;
            var status = _client.GetRefundStatus(orderNumber);

            await _responder.ReplyWith(stepContext.Context, ReturnResponses.ResponseIds.RefundStatusMessage);
            await _responder.ReplyWith(stepContext.Context, ReturnResponses.ResponseIds.RefundStatusCard, status);

            return await stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string OrderNumberPrompt = "orderNumberPrompt";
            public const string PhoneNumberPrompt = "phoneNumberPrompt";
        }
    }
}
