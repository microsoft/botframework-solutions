// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace Microsoft.Bot.Solutions.Authentication
{
    /// <summary>
    /// Provides the ability to prompt for which Authentication provider the user wishes to use.
    /// </summary>
    public class MultiProviderAuthDialog : ComponentDialog
    {
        private static readonly string[] AcceptedLocales = new string[] { "en", "de", "es", "fr", "it", "zh" };
        private string _selectedAuthType = string.Empty;
        private List<OAuthConnection> _authenticationConnections;
        private ResponseManager _responseManager;
        private AppCredentials _oauthCredentials;

        public MultiProviderAuthDialog(
            List<OAuthConnection> authenticationConnections,
            List<OAuthPromptSettings> promptSettings = null,
            AppCredentials oauthCredentials = null)
            : base(nameof(MultiProviderAuthDialog))
        {
            _authenticationConnections = authenticationConnections ?? throw new ArgumentNullException(nameof(authenticationConnections));
            _oauthCredentials = oauthCredentials;
            _responseManager = new ResponseManager(
                AcceptedLocales,
                new AuthenticationResponses());

            var firstStep = new WaterfallStep[]
            {
                FirstStepAsync,
            };

            var authSteps = new WaterfallStep[]
            {
                PromptForProviderAsync,
                PromptForAuthAsync,
                HandleTokenResponseAsync,
            };

            AddDialog(new WaterfallDialog(DialogIds.FirstStepPrompt, firstStep));

            if (_authenticationConnections != null &&
                _authenticationConnections.Count > 0 &&
                _authenticationConnections.Any(c => !string.IsNullOrWhiteSpace(c.Name)))
            {
                for (int i = 0; i < _authenticationConnections.Count; ++i)
                {
                    var connection = _authenticationConnections[i];

                    foreach (var locale in AcceptedLocales)
                    {
                        // We ignore placeholder connections in config that don't have a Name
                        if (!string.IsNullOrWhiteSpace(connection.Name))
                        {
                            AddDialog(GetLocalizedDialog(locale, connection.Name, promptSettings?[i]));
                        }
                    }
                }

                AddDialog(new WaterfallDialog(DialogIds.AuthPrompt, authSteps));
                AddDialog(new ChoicePrompt(DialogIds.ProviderPrompt));
            }
            else
            {
                throw new ArgumentNullException(nameof(authenticationConnections));
            }
        }

        // Validators
        protected Task<bool> TokenResponseValidatorAsync(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null &&
               ((activity.Type == ActivityTypes.Event && activity.Name == TokenEvents.TokenResponseEventName) ||
               (activity.Type == ActivityTypes.Invoke && activity.Name == "signin/verifyState")))
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private async Task<DialogTurnResult> FirstStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(DialogIds.AuthPrompt).ConfigureAwait(false);
        }

        private async Task<DialogTurnResult> PromptForProviderAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (_authenticationConnections.Count == 1)
            {
                var result = _authenticationConnections.First().Name + "_" + CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return await stepContext.NextAsync(result).ConfigureAwait(false);
            }

            if (stepContext.Context.Adapter is IExtendedUserTokenProvider adapter)
            {
                var tokenStatusCollection = await adapter.GetTokenStatusAsync(stepContext.Context, _oauthCredentials, stepContext.Context.Activity.From.Id, null, cancellationToken).ConfigureAwait(false);

                var matchingProviders = tokenStatusCollection.Where(p => (bool)p.HasToken && _authenticationConnections.Any(t => t.Name == p.ConnectionName)).ToList();

                if (matchingProviders.Count == 1)
                {
                    var authType = matchingProviders[0].ConnectionName;
                    return await stepContext.NextAsync(authType, cancellationToken).ConfigureAwait(false);
                }

                if (matchingProviders.Count > 1)
                {
                    var choices = new List<Choice>();

                    foreach (var connection in matchingProviders)
                    {
                        choices.Add(new Choice
                        {
                            Action = new CardAction(ActionTypes.ImBack, connection.ConnectionName, value: connection.ConnectionName),
                            Value = connection.ConnectionName,
                        });
                    }

                    return await stepContext.PromptAsync(
                        DialogIds.ProviderPrompt,
                        new PromptOptions
                        {
                            Prompt = _responseManager.GetResponse(AuthenticationResponses.ConfiguredAuthProvidersPrompt),
                            Choices = choices,
                        },
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var choices = new List<Choice>();

                    foreach (var connection in _authenticationConnections)
                    {
                        choices.Add(new Choice
                        {
                            Action = new CardAction(ActionTypes.ImBack, connection.Name, value: connection.Name),
                            Value = connection.Name,
                        });
                    }

                    return await stepContext.PromptAsync(
                        DialogIds.ProviderPrompt,
                        new PromptOptions
                        {
                            Prompt = _responseManager.GetResponse(AuthenticationResponses.AuthProvidersPrompt),
                            Choices = choices,
                        },
                        cancellationToken).ConfigureAwait(false);
                }
            }

            throw new Exception("The adapter doesn't support Token Handling.");
        }

        private async Task<DialogTurnResult> PromptForAuthAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is string selectedAuthType)
            {
                _selectedAuthType = selectedAuthType;
            }
            else if (stepContext.Result is FoundChoice choice)
            {
                _selectedAuthType = choice.Value;
            }

            return await stepContext.PromptAsync(_selectedAuthType, new PromptOptions(), cancellationToken).ConfigureAwait(false);
        }

        private async Task<DialogTurnResult> HandleTokenResponseAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result is TokenResponse tokenResponse && !string.IsNullOrWhiteSpace(tokenResponse.Token))
            {
                var result = await CreateProviderTokenResponseAsync(stepContext.Context, tokenResponse).ConfigureAwait(false);

                return await stepContext.EndDialogAsync(result, cancellationToken).ConfigureAwait(false);
            }

            TelemetryClient.TrackEvent("TokenRetrievalFailure");
            return new DialogTurnResult(DialogTurnStatus.Cancelled);
        }

        private async Task<ProviderTokenResponse> CreateProviderTokenResponseAsync(ITurnContext context, TokenResponse tokenResponse)
        {
            var tokens = await GetTokenStatusAsync(context, context.Activity.From.Id).ConfigureAwait(false);
            var match = Array.Find(tokens, t => t.ConnectionName == tokenResponse.ConnectionName);

            return new ProviderTokenResponse
            {
                AuthenticationProvider = match.ServiceProviderDisplayName.GetAuthenticationProvider(),
                TokenResponse = tokenResponse,
            };
        }

        private async Task<TokenStatus[]> GetTokenStatusAsync(ITurnContext context, string userId, string includeFilter = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            BotAssert.ContextNotNull(context);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var tokenProvider = context.Adapter as IExtendedUserTokenProvider;
            if (tokenProvider != null)
            {
                return await tokenProvider.GetTokenStatusAsync(context, _oauthCredentials, userId, includeFilter, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new Exception("Adapter does not support IExtendedUserTokenProvider");
            }
        }

        private Task<bool> AuthPromptValidatorAsync(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null && !string.IsNullOrWhiteSpace(token.Token))
            {
                return Task.FromResult(true);
            }

            var eventActivity = promptContext.Context.Activity.AsEventActivity();
            if (eventActivity != null && eventActivity.Name == TokenEvents.TokenResponseEventName)
            {
                promptContext.Recognized.Value = eventActivity.Value as TokenResponse;
                return Task.FromResult(true);
            }

            TelemetryClient.TrackEvent("AuthPromptValidatorAsyncFailure");
            return Task.FromResult(false);
        }

        private OAuthPrompt GetLocalizedDialog(string locale, string connectionName, OAuthPromptSettings settings)
        {
            var loginButtonActivity = _responseManager.GetResponse(AuthenticationResponses.LoginButton, locale);
            var loginPromptActivity = _responseManager.GetResponse(AuthenticationResponses.LoginPrompt, locale, new StringDictionary() { { "authType", connectionName } });
            settings = settings ?? new OAuthPromptSettings
            {
                ConnectionName = connectionName,
                Title = loginButtonActivity.Text,
                Text = loginPromptActivity.Text,
            };
            settings.OAuthAppCredentials = _oauthCredentials;

            return new OAuthPrompt(
                connectionName + "_" + locale,
                settings,
                AuthPromptValidatorAsync);
        }

        private static class DialogIds
        {
            public const string ProviderPrompt = "ProviderPrompt";
            public const string FirstStepPrompt = "FirstStep";
            public const string AuthPrompt = "AuthPrompt";
        }
    }
}