using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Orders
{
    public class CheckOrderStatusDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private OrderResponses _responder = new OrderResponses();

        public CheckOrderStatusDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor,
            IBotTelemetryClient telemetryClient)
            : base(services, nameof(CheckOrderStatusDialog), telemetryClient)
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;
            TelemetryClient = telemetryClient;

            var checkOrderStatus = new WaterfallStep[]
            {
                PromptForOrderNumber,
                PromptForPhoneNumber,
                ShowOrderStatus,
            };

            InitialDialogId = nameof(CheckOrderStatusDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, checkOrderStatus) { TelemetryClient = telemetryClient });
            AddDialog(new TextPrompt(DialogIds.OrderNumberPrompt, SharedValidators.OrderNumberValidator));
            AddDialog(new TextPrompt(DialogIds.PhoneNumberPrompt, SharedValidators.PhoneNumberValidator));
        }

        private async Task<DialogTurnResult> PromptForOrderNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.OrderNumberPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.OrderNumberPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.OrderNumberReprompt),
            });
        }

        private async Task<DialogTurnResult> PromptForPhoneNumber(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CustomerSupportTemplateState());

            var orderNumber = (string)stepContext.Result;
            var order = state.Order = _client.GetOrderByNumber(orderNumber);

            return await stepContext.PromptAsync(DialogIds.PhoneNumberPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.PhoneNumberPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, OrderResponses.ResponseIds.PhoneNumberReprompt)
            });
        }

        private async Task<DialogTurnResult> ShowOrderStatus(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CustomerSupportTemplateState());
            var order = state.Order;

            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.OrderStatusMessage);
            await _responder.ReplyWith(stepContext.Context, OrderResponses.ResponseIds.OrderStatusCard, state.Order);
            return await stepContext.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string OrderNumberPrompt = "orderNumberPrompt";
            public const string PhoneNumberPrompt = "phoneNumberPrompt";
        }
    }
}
