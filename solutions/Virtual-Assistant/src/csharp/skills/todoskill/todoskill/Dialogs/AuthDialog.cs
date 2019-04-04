using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Authentication;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Telemetry;
using Microsoft.Bot.Schema;
using ToDoSkill.Models;

namespace ToDoSkill.Dialogs
{
    public class AuthDialog : ComponentDialog
    {
        private ResponseManager _responseManager;
        private Dictionary<string, CognitiveModelSet> _cognitiveModels;
        private List<OAuthConnection> _oauthConnections;
        private string _selectedAuthType = string.Empty;

        public AuthDialog(Dictionary<string, CognitiveModelSet> cognitiveModels, List<OAuthConnection> oauthConnections, bool authenticationRequired = false)
            : base(nameof(AuthDialog))
        {
            _cognitiveModels = cognitiveModels;
            _oauthConnections = oauthConnections;
            _responseManager = new ResponseManager(cognitiveModels.Keys.ToArray(), new AuthenticationResponses());

            if (authenticationRequired && !oauthConnections.Any())
            {
                throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            }

            var auth = new WaterfallStep[]
            {
                PromptForProvider,
                PromptForAuth,
                HandleTokenResponse,
            };

            AddDialog(new WaterfallDialog(nameof(AuthDialog), auth));
            AddDialog(new ChoicePrompt(DialogIds.ProviderPrompt) { Style = ListStyle.SuggestedAction });

            foreach (var connection in oauthConnections)
            {
                AddDialog(new OAuthPrompt(
                    connection.Name,
                    new OAuthPromptSettings
                    {
                        ConnectionName = connection.Name,
                        Title = CommonStrings.Login,
                        Text = string.Format(CommonStrings.LoginDescription, connection.Name),
                    },
                    AuthPromptValidator));
            }
        }

        private async Task<DialogTurnResult> PromptForProvider(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (_oauthConnections.Count() == 1)
            {
                var result = _oauthConnections.ElementAt(0).Name;
                return await stepContext.NextAsync(result);
            }
            else
            {
                var adapter = stepContext.Context.Adapter as BotFrameworkAdapter;
                var tokenStatusCollection = await adapter.GetTokenStatusAsync(stepContext.Context, stepContext.Context.Activity.From.Id);

                var matchingProviders = tokenStatusCollection.Where(p => (bool)p.HasToken && _oauthConnections.Any(t => t.Name == p.ConnectionName)).ToList();

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
                        Prompt = _responseManager.GetResponse(AuthenticationResponses.ConfiguredAuthProvidersPrompt),
                        Choices = choices,
                    });
                }
                else
                {
                    var choices = new List<Choice>();

                    foreach (var connection in _oauthConnections)
                    {
                        choices.Add(new Choice()
                        {
                            Action = new CardAction(ActionTypes.ImBack, connection.Name, value: connection.Name),
                            Value = connection.Name,
                        });
                    }

                    return await stepContext.PromptAsync(DialogIds.ProviderPrompt, new PromptOptions
                    {
                        Prompt = _responseManager.GetResponse(AuthenticationResponses.AuthProvidersPrompt),
                        Choices = choices,
                    });
                }
            }
        }

        private async Task<DialogTurnResult> PromptForAuth(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is string)
            {
                _selectedAuthType = stepContext.Result as string;
            }
            else if (stepContext.Result is FoundChoice)
            {
                var choice = stepContext.Result as FoundChoice;
                _selectedAuthType = choice.Value;
            }

            return await stepContext.PromptAsync(_selectedAuthType, new PromptOptions());
        }

        private async Task<DialogTurnResult> HandleTokenResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokenResponse = stepContext.Result as TokenResponse;
            if (tokenResponse != null && !string.IsNullOrWhiteSpace(tokenResponse.Token))
            {
                var result = await CreateProviderTokenResponse(stepContext.Context, tokenResponse);

                return await stepContext.EndDialogAsync(result);
            }
            else
            {
                TelemetryClient.TrackEventEx("TokenRetrievalFailure", stepContext.Context.Activity);
                return new DialogTurnResult(DialogTurnStatus.Cancelled);
            }
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
            if (token != null && !string.IsNullOrWhiteSpace(token.Token))
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