using CustomerSupportTemplate.Dialogs.Account.Resources;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Account
{
    public class PayBillDialog : CustomerSupportDialog
    {
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private AccountResponses _responder = new AccountResponses();

        public PayBillDialog(
            BotServices services, 
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor,
            IBotTelemetryClient telemetryClient)
            : base(services, nameof(PayBillDialog), telemetryClient)
        {
            _services = services;
            _stateAccessor = stateAccessor;
            TelemetryClient = telemetryClient;

            var payBill = new WaterfallStep[]
            {
                ShowPayBillOptions
            };

            InitialDialogId = nameof(PayBillDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, payBill) { TelemetryClient = telemetryClient });
        }

        private async Task<DialogTurnResult> ShowPayBillOptions(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(sc.Context, AccountResponses.ResponseIds.PayBillPolicyCard);
            return await sc.EndDialogAsync();
        }
    }
}
