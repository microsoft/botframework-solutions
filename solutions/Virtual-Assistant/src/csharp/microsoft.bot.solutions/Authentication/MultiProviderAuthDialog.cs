﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;

namespace Microsoft.Bot.Solutions.Authentication
{
    public class MultiProviderAuthDialog : ComponentDialog
    {
        private ISkillConfiguration _skillConfiguration;
        private CommonResponseBuilder _responseBuilder = new CommonResponseBuilder();

        public MultiProviderAuthDialog(ISkillConfiguration skillConfiguration)
            : base(nameof(MultiProviderAuthDialog))
        {
            _skillConfiguration = skillConfiguration;

            if (_skillConfiguration.IsAuthenticatedSkill && !_skillConfiguration.AuthenticationConnections.Any())
            {
                throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            }

            var auth = new WaterfallStep[]
            {
                PromptForProvider,
                PromptForAuth,
                HandleTokenResponse,
            };

            AddDialog(new WaterfallDialog(nameof(MultiProviderAuthDialog), auth));
            AddDialog(new ChoicePrompt(DialogIds.ProviderPrompt) { Style = ListStyle.SuggestedAction });

            foreach (var connection in skillConfiguration.AuthenticationConnections)
            {
                AddDialog(new OAuthPrompt(
                    connection.Key,
                    new OAuthPromptSettings
                    {
                        ConnectionName = connection.Key,
                        Title = CommonStrings.Login,
                        Text = string.Format(CommonStrings.LoginDescription, connection.Key),
                        Timeout = 30000,
                    },
                    AuthPromptValidator));
            }
        }

        private async Task<DialogTurnResult> PromptForProvider(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (_skillConfiguration.AuthenticationConnections.Count() == 1)
            {
                var result = _skillConfiguration.AuthenticationConnections.ElementAt(0).Key;
                return await stepContext.NextAsync(result);
            }
            else
            {
                var adapter = stepContext.Context.Adapter as BotFrameworkAdapter;
                var tokenStatusCollection = await adapter.GetTokenStatusAsync(stepContext.Context, stepContext.Context.Activity.From.Id);

                var matchingProviders = tokenStatusCollection.Where(p => p.HasToken && _skillConfiguration.AuthenticationConnections.Any(t => t.Key == p.ConnectionName)).ToList();

                if (matchingProviders.Count() == 1)
                {
                    var authType = matchingProviders[0].ConnectionName;
                    return await stepContext.NextAsync(authType);
                }
                else if (matchingProviders.Count() > 1)
                {
                    var choices = new List<Choice>();

                    foreach (var connection in matchingProviders)
                    {
                        choices.Add(new Choice()
                        {
                            Action = new CardAction(ActionTypes.ImBack, connection.ConnectionName, value: connection.ConnectionName),
                            Value = connection.ConnectionName,
                        });
                    }

                    return await stepContext.PromptAsync(DialogIds.ProviderPrompt, new PromptOptions
                    {
                        Prompt = stepContext.Context.Activity.CreateReply(CommonResponses.ConfiguredAuthProvidersPrompt),
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
                        Prompt = stepContext.Context.Activity.CreateReply(CommonResponses.AuthProvidersPrompt),
                        Choices = choices,
                    });
                }
            }
        }

        private async Task<DialogTurnResult> PromptForAuth(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var authType = string.Empty;
            if (stepContext.Result is string)
            {
                authType = stepContext.Result as string;
            }
            else if (stepContext.Result is FoundChoice)
            {
                var choice = stepContext.Result as FoundChoice;
                authType = choice.Value;
            }

            return await stepContext.PromptAsync(authType, new PromptOptions());
        }

        private async Task<DialogTurnResult> HandleTokenResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokenResponse = stepContext.Result as TokenResponse;
            var result = await CreateProviderTokenResponse(stepContext.Context, tokenResponse);

            return await stepContext.EndDialogAsync(result);
        }

        private async Task<ProviderTokenResponse> CreateProviderTokenResponse(ITurnContext context, TokenResponse tokenResponse)
        {
            var adapter = context.Adapter as BotFrameworkAdapter;
            var tokens = await adapter.GetTokenStatusAsync(context, context.Activity.From.Id);
            var match = Array.Find(tokens, t => t.ConnectionName == tokenResponse.ConnectionName);

            return new ProviderTokenResponse
            {
                AuthenticationProvider = match.ServiceProviderDisplayName.GetAuthenticationProvider(),
                TokenResponse = tokenResponse,
            };
        }

        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        private class DialogIds
        {
            public const string ProviderPrompt = "ProviderPrompt";
        }
    }
}
