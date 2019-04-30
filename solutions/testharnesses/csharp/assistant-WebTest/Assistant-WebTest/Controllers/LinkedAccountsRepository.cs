// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Assistant_WebTest.Controllers
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.BotFramework;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;

    public class LinkedAccountRepository : ILinkedAccountRepository
    {
        private const string TokenServiceUrl = "https://api.botframework.com";

        /// <summary>
        /// Enumerate the Linked Account status for a given UserId and return status information
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="credentialProvider"></param>
        /// <returns></returns>
        public async Task<TokenStatus[]> GetTokenStatusAsync(string userId, ICredentialProvider credentialProvider)
        {
            // The BotFramework Adapter, Bot ApplicationID and Bot Secret is required to access the Token APIs
            // These must match the Bot making use of the Linked Accounts feature.

            var adapter = new BotFrameworkAdapter(credentialProvider);
            var botAppId = ((ConfigurationCredentialProvider)credentialProvider).AppId;
            var botAppPassword = ((ConfigurationCredentialProvider)credentialProvider).Password;

            TokenStatus[] tokenStatuses = null;

            using (var context = new TurnContext(adapter, new Microsoft.Bot.Schema.Activity { }))
            {
                var connectorClient = new ConnectorClient(new Uri(TokenServiceUrl), botAppId, botAppPassword);
                context.TurnState.Add<IConnectorClient>(connectorClient);

                // Retrieve the Token Status
                tokenStatuses = await adapter.GetTokenStatusAsync(context, userId);
            }

            return tokenStatuses;
        }

        /// <summary>
        /// Retrieve a signin link for a user based on the Connection Name. This is then used for the user to click and authenticate, generating a token returned back to the Token store
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="credentialProvider"></param>
        /// <param name="connectionName"></param>
        /// <param name="finalRedirect"></param>
        /// <returns></returns>
        public async Task<string> GetSignInLinkAsync(string userId, ICredentialProvider credentialProvider, string connectionName, string finalRedirect)
        {
            // The BotFramework Adapter, Bot ApplicationID and Bot Secret is required to access the Token APIs
            // These must match the Bot making use of the Linked Accounts feature.

            var adapter = new BotFrameworkAdapter(credentialProvider);
            var botAppId = ((ConfigurationCredentialProvider)credentialProvider).AppId;
            var botAppPassword = ((ConfigurationCredentialProvider)credentialProvider).Password;

            string link = null;
            using (var context = new TurnContext(adapter, new Microsoft.Bot.Schema.Activity { }))
            {
                var connectorClient = new ConnectorClient(new Uri(TokenServiceUrl), botAppId, botAppPassword);
                context.TurnState.Add<IConnectorClient>(connectorClient);

                // Retrieve a signin link for a given Connection Name and UserId
                link = await adapter.GetOauthSignInLinkAsync(context, connectionName, userId, finalRedirect);

                // Add on code_challenge (SessionId) into the redirect
                var sessionId = SessionController.Sessions.FirstOrDefault(s => s.Key == userId).Value;

                if (!string.IsNullOrEmpty(sessionId))
                {
                    link += HttpUtility.UrlEncode($"&code_challenge={sessionId}");
                }
            }
            return link;
        }

        /// <summary>
        /// Sign a given user out of a previously linked account
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="credentialProvider"></param>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public async Task SignOutAsync(string userId, ICredentialProvider credentialProvider, string connectionName = null)
        {
            // The BotFramework Adapter, Bot ApplicationID and Bot Secret is required to access the Token APIs
            // These must match the Bot making use of the Linked Accounts feature.
            var adapter = new BotFrameworkAdapter(credentialProvider);
            var botAppId = ((ConfigurationCredentialProvider)credentialProvider).AppId;
            var botAppPassword = ((ConfigurationCredentialProvider)credentialProvider).Password;

            using (var context = new TurnContext(adapter, new Microsoft.Bot.Schema.Activity { }))
            {
                var connectorClient = new ConnectorClient(new Uri(TokenServiceUrl), botAppId, botAppPassword);
                context.TurnState.Add<IConnectorClient>(connectorClient);

                // Sign the specified user out of a particular connection
                await adapter.SignOutUserAsync(context, connectionName, userId);
            }
        }
    }
}
