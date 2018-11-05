using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Linq;
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
            AddDialog(new ChoicePrompt(DialogIds.ProviderPrompt) { Style = ListStyle.SuggestedAction });

            foreach (var connection in skillConfiguration.AuthenticationConnections)
            {
                AddDialog(new OAuthPrompt(connection.Key, new OAuthPromptSettings
                {
                    ConnectionName = connection.Value,
                    Title = "Login",
                    Text = $"Please login with your {connection.Key} account.",
                    Timeout = 30000,
                }));
            }
        }

        private async Task<DialogTurnResult> PromptForProvider(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var adapter = stepContext.Context.Adapter as BotFrameworkAdapter;
            var tokenStatusCollection = await adapter.GetTokenStatusAsync(stepContext.Context, stepContext.Context.Activity.From.Id);

            var matchingProviders = tokenStatusCollection.Where(p => p.HasToken && _skillConfiguration.AuthenticationConnections.Any(t => t.Value == p.ConnectionName)).ToList();

            if (matchingProviders.Count() == 1)
            {
                var authType = matchingProviders[0].ServiceProviderDisplayName;
                stepContext.Values["provider"] = authType;
                return await stepContext.NextAsync();
            }
            else if (matchingProviders.Count() > 1)
            {
                var choices = new List<Choice>();

                foreach (var connection in matchingProviders)
                {
                    choices.Add(new Choice()
                    {
                        Action = new CardAction(ActionTypes.ImBack, connection.ServiceProviderDisplayName, value: connection.ServiceProviderDisplayName),
                        Value = connection.ServiceProviderDisplayName,
                    });
                }

                return await stepContext.PromptAsync(DialogIds.ProviderPrompt, new PromptOptions
                {
                    Prompt = new Activity(type: ActivityTypes.Message, text: "You have multiple accounts configured. Which one would you like to use?"),
                    Choices = choices,
                });
            }
            else
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

                return await stepContext.PromptAsync(DialogIds.ProviderPrompt, new PromptOptions
                {
                    Prompt = new Activity(type: ActivityTypes.Message, text: "Which account do you want to use?"),
                    Choices = choices,
                });
            }
        }

        private async Task<DialogTurnResult> PromptForAuth(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values.TryGetValue("provider", out var authType);

            if (authType == null && stepContext.Result != null)
            {
                var choice = stepContext.Result as FoundChoice;
                authType = choice.Value;
            }

            return await stepContext.PromptAsync((string)authType, new PromptOptions());
        }

        private async Task<DialogTurnResult> HandleTokenResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = stepContext.Result as TokenResponse;
            return await stepContext.EndDialogAsync(result);
        }

        private class DialogIds
        {
            public const string ProviderPrompt = "ProviderPrompt";
        }
    }
}
