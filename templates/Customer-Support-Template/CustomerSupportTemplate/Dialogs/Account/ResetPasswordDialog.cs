using CustomerSupportTemplate.Dialogs.Account.Resources;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Account
{
    public class ResetPasswordDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private AccountResponses _responder = new AccountResponses();

        public ResetPasswordDialog(
            BotServices services,
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor,
            IBotTelemetryClient telemetryClient)
            : base(services, nameof(ResetPasswordDialog), telemetryClient)
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;
            TelemetryClient = telemetryClient;

            var resetPassword = new WaterfallStep[]
            {
                AskForAccountId,
                AskForEmail,
                CompleteDialog,
            };

            InitialDialogId = nameof(ResetPasswordDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, resetPassword) { TelemetryClient = telemetryClient });
            AddDialog(new TextPrompt(DialogIds.AccountIdPrompt));
            AddDialog(new TextPrompt(DialogIds.EmailPrompt, EmailValidator));
        }

        private async Task<DialogTurnResult> AskForAccountId(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.AccountIdPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, AccountResponses.ResponseIds.AccountIdPrompt),
            });
        }

        private async Task<DialogTurnResult> AskForEmail(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CustomerSupportTemplateState());
            var id = (string)stepContext.Result;
            var account = state.Account = _client.GetAccountById(id);

            return await stepContext.PromptAsync(DialogIds.EmailPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, AccountResponses.ResponseIds.EmailPrompt),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, AccountResponses.ResponseIds.InvalidEmailMessage),
            });
        }

        private async Task<DialogTurnResult> CompleteDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CustomerSupportTemplateState());
            var email = (string)stepContext.Result;
            var id = state.Account.Id;

            // Uncomment these lines to verify email is correct.
            //if(email == state.Account.Email)
            //{
                _client.SendPasswordResetEmail(id);
            //}

            await _responder.ReplyWith(stepContext.Context, AccountResponses.ResponseIds.ResetEmailSentMessage, email);

            return await stepContext.EndDialogAsync();
        }

        private Task<bool> EmailValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var regex = new Regex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", RegexOptions.IgnoreCase);

            var match = regex.Match(promptContext.Recognized.Value);

            if (match.Success)
            {
                promptContext.Recognized.Value = match.Value;
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        private class DialogIds
        {
            public const string AccountIdPrompt = "accountIdPrompt";
            public const string EmailPrompt = "emailPrompt";
        }
    }
}
