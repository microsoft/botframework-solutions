// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using EnterpriseBotSample.Dialogs.Authentication.Resources;
using EnterpriseBotSample.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Graph;

namespace EnterpriseBotSample.Dialogs.Shared
{
    public class AuthenticationDialog : ComponentDialog
    {
        private static AuthenticationResponses _responder = new AuthenticationResponses();

        public AuthenticationDialog(string connectionName)
            : base(nameof(AuthenticationDialog))
        {
            InitialDialogId = nameof(AuthenticationDialog);
            ConnectionName = connectionName;

            var authenticate = new WaterfallStep[]
            {
                PromptToLogin,
                FinishLoginDialog,
            };

            AddDialog(new WaterfallDialog(InitialDialogId, authenticate));
            AddDialog(new OAuthPrompt(DialogIds.LoginPrompt, new OAuthPromptSettings()
            {
                ConnectionName = ConnectionName,
                Title = AuthenticationStrings.TITLE,
                Text = AuthenticationStrings.PROMPT,
            }));
        }

        private string ConnectionName { get; set; }

        private async Task<DialogTurnResult> PromptToLogin(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.PromptAsync(AuthenticationResponses.ResponseIds.LoginPrompt, new PromptOptions());
        }

        private async Task<DialogTurnResult> FinishLoginDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var activity = sc.Context.Activity;
            if (sc.Result != null)
            {
                var tokenResponse = sc.Result as TokenResponse;

                if (tokenResponse?.Token != null)
                {
                    var user = await GetProfile(sc.Context, tokenResponse);
                    await _responder.ReplyWith(sc.Context, AuthenticationResponses.ResponseIds.SucceededMessage, new { name = user.DisplayName });
                    return await sc.EndDialogAsync(tokenResponse);
                }
            }
            else
            {
                await _responder.ReplyWith(sc.Context, AuthenticationResponses.ResponseIds.FailedMessage);
            }

            return await sc.EndDialogAsync();
        }

        private async Task<User> GetProfile(ITurnContext context, TokenResponse tokenResponse)
        {
            var token = tokenResponse;
            var client = new GraphClient(token.Token);

            return await client.GetMe();
        }

        private class DialogIds
        {
            public const string LoginPrompt = "loginPrompt";
        }
    }
}
