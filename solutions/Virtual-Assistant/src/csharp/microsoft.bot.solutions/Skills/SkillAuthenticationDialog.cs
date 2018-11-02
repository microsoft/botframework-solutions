using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillAuthenticationDialog : ComponentDialog
    {
        private ISkillConfiguration _skillConfiguration;

        public SkillAuthenticationDialog(ISkillConfiguration skillConfiguration)
            : base(nameof(SkillAuthenticationDialog))
        {
            _skillConfiguration = skillConfiguration;

            var auth = new WaterfallStep[]
            {
                PromptForProvider,
                PromptForAuth,
                HandleTokenResponse,
            };

            AddDialog(new WaterfallDialog(nameof(SkillAuthenticationDialog), auth));
            AddDialog(new ChoicePrompt("ProviderPrompt") { Style = ListStyle.SuggestedAction });

            foreach (var connection in skillConfiguration.AuthenticationConnections)
            {
                AddDialog(new OAuthPrompt(connection.Key, new OAuthPromptSettings
                {
                    ConnectionName = connection.Value,
                    Text = $"Please login with your {connection.Key} account.",
                    Timeout = 30000,
                }));
            }
        }

        private async Task<DialogTurnResult> PromptForProvider(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choices = new List<Choice>();

            foreach (var connection in _skillConfiguration.AuthenticationConnections)
            {
                choices.Add(new Choice()
                {
                    Action = new CardAction(ActionTypes.ImBack, connection.Key, value: connection.Key),
                    Value = connection.Key,
                });
            }

            return await stepContext.PromptAsync("ProviderPrompt", new PromptOptions
            {
                Prompt = new Activity(type: ActivityTypes.Message, text: "Which account would you like to use?"),
                Choices = choices,
            });
        }

        private async Task<DialogTurnResult> PromptForAuth(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var choice = stepContext.Result as FoundChoice;
            var authType = choice.Value;
            return await stepContext.PromptAsync(authType, new PromptOptions());
        }

        private async Task<DialogTurnResult> HandleTokenResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = stepContext.Result as TokenResponse;
            return await stepContext.EndDialogAsync(result);
        }
    }
}
