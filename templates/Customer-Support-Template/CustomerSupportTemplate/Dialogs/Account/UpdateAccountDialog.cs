using CustomerSupportTemplate.Dialogs.Account.Resources;
using CustomerSupportTemplate.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
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
            IStatePropertyAccessor<CustomerSupportTemplateState> stateAccessor)
            : base(services, nameof(UpdateAccountDialog))
        {
            _client = new DemoServiceClient();
            _services = services;
            _stateAccessor = stateAccessor;

            var updateAccount = new WaterfallStep[]
            {
                ShowSteps,
                PromptToLogin,
                PromptForUpdatedInfo,
                CompleteDialog,
            };

            InitialDialogId = nameof(UpdateAccountDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, updateAccount));
            AddDialog(new TextPrompt(DialogIds.UpdateContactInfoPrompt, ContactInfoValidator));
            AddDialog(new OAuthPrompt(DialogIds.AuthPrompt, new OAuthPromptSettings()
            {
                ConnectionName = services.AuthConnectionName,
                Text = UpdateAccountStrings.LoginPrompt,
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
            });
        }

        private async Task<DialogTurnResult> CompleteDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var info = (Models.Account)stepContext.Result;
            _client.UpdateUserContactInfo(info);
            await _responder.ReplyWith(stepContext.Context, AccountResponses.ResponseIds.NewInfoSavedPrompt);

            return await stepContext.EndDialogAsync();
        }

        private Task<bool> ContactInfoValidator(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        private class DialogIds
        {
            public const string UpdateContactInfoPrompt = "updateContactInfo";
            public const string AuthPrompt = "authPrompt";
        }
    }
}
