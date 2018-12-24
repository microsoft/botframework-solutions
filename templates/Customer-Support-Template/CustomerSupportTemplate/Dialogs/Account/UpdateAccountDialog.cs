using CustomerSupportTemplate.Dialogs.Account.Resources;
using CustomerSupportTemplate.Dialogs.Shared;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerSupportTemplate.Dialogs.Account
{
    public class UpdateAccountDialog : CustomerSupportDialog
    {
        private IServiceClient _client;
        private BotServices _services;
        private IStatePropertyAccessor<CustomerSupportTemplateState> _stateAccessor;
        private AccountResponses _responder = new AccountResponses();

        public UpdateAccountDialog(
            BotServices services,
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor,
            IBotTelemetryClient telemetryClient)
            : base(services, nameof(UpdateAccountDialog), telemetryClient)
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;
            TelemetryClient = telemetryClient;

            var updateAccount = new WaterfallStep[]
            {
                ShowSteps,
                PromptToLogin,
                PromptForUpdatedInfo,
                CompleteDialog,
            };

            InitialDialogId = nameof(UpdateAccountDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, updateAccount) { TelemetryClient = telemetryClient });
            AddDialog(new FormPrompt(DialogIds.UpdateContactInfoPrompt, ContactInfoValidator));
            AddDialog(new OAuthPrompt(DialogIds.AuthPrompt, new OAuthPromptSettings()
            {
                ConnectionName = services.AuthConnectionName,
                Text = AccountStrings.LoginPrompt,
                Title = "Login",
                Timeout = 30000,
            }));
        }

        private async Task<DialogTurnResult> ShowSteps(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // send steps
            await _responder.ReplyWith(stepContext.Context, AccountResponses.ResponseIds.UpdateContactInfoMessage);
            return await stepContext.NextAsync();
        }

        private async Task<DialogTurnResult> PromptToLogin(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(DialogIds.AuthPrompt, new PromptOptions());
        }

        private async Task<DialogTurnResult> PromptForUpdatedInfo(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await _responder.ReplyWith(stepContext.Context, AccountResponses.ResponseIds.NewInfoPrompt);

            return await stepContext.PromptAsync(DialogIds.UpdateContactInfoPrompt, new PromptOptions
            {
                Prompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, AccountResponses.ResponseIds.NewInfoCard),
                RetryPrompt = await _responder.RenderTemplate(stepContext.Context, stepContext.Context.Activity.Locale, AccountResponses.ResponseIds.NewInfoReprompt),
            });
        }

        private async Task<DialogTurnResult> CompleteDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            dynamic value = (stepContext.Result as Activity).Value;
            var info = new Models.Account()
            {
                Name = value.name,
                Email = value.email,
                Phone = value.phone,
            };

            _client.UpdateUserContactInfo(info);
            await _responder.ReplyWith(stepContext.Context, AccountResponses.ResponseIds.NewInfoSavedPrompt);

            return await stepContext.EndDialogAsync();
        }

        private Task<bool> ContactInfoValidator(PromptValidatorContext<Activity> promptContext, CancellationToken cancellationToken)
        {
            dynamic value = promptContext.Context.Activity.Value;

            if (value != null)
            {
                if (!string.IsNullOrEmpty((string)value.name) || !string.IsNullOrEmpty((string)value.email) || !string.IsNullOrEmpty((string)value.phone))
                {
                    return Task.FromResult(true);
                }
            }

            // start the waterfall dialog for updated info
            return Task.FromResult(false);
        }

        private class DialogIds
        {
            public const string UpdateContactInfoPrompt = "updateContactInfo";
            public const string AuthPrompt = "authPrompt";
        }
    }
}
