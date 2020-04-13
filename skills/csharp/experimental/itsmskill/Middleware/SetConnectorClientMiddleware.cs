namespace ITSMSkill.Middleware
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Rest.TransientFaultHandling;

    public class SetConnectorClientMiddleware : IMiddleware
    {
        private const string BotIdentityKey = "BotIdentity";

        /// <summary>
        /// The application credential map. This is used to ensure we don't try to get tokens for Bot every time.
        /// </summary>
        private readonly ConcurrentDictionary<string, MicrosoftAppCredentials> appCredentialMap = new ConcurrentDictionary<string, MicrosoftAppCredentials>();

        /// <summary>
        /// The credential provider.
        /// </summary>
        private readonly ICredentialProvider credentialProvider;

        public SetConnectorClientMiddleware(ICredentialProvider credentialProvider)
        {
            this.credentialProvider = credentialProvider;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            // Use the ServiceUrl of the Activity, fallback with the default ServiceUrl constant.
            var serviceUrl = turnContext.Activity.ServiceUrl;

            BotAssert.ContextNotNull(turnContext);

            // BotFrameworkAdapter when processing activity, post Auth adds BotIdentity into the context.
            // If we failed to find ClaimsIdentity, create a new AnonymousIdentity. This tells us that Auth is off.
            ClaimsIdentity claimsIdentity = turnContext.TurnState.Get<ClaimsIdentity>(BotIdentityKey) ?? new ClaimsIdentity(new List<Claim>(), "anonymous");

            // For requests from channel App Id is in Audience claim of JWT token. For emulator it is in AppId claim. For
            // unauthenticated requests we have anonymous identity provided auth is disabled.
            Claim botAppIdClaim = claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AudienceClaim)
                ??
                claimsIdentity.Claims?.SingleOrDefault(claim => claim.Type == AuthenticationConstants.AppIdClaim);

            if (botAppIdClaim != null)
            {
                string appId = botAppIdClaim.Value;
                MicrosoftAppCredentials credentials = await this.GetAppCredentialsAsync(appId);
                var connectorClient = new ConnectorClient(new Uri(serviceUrl), credentials);

                MicrosoftAppCredentials.TrustServiceUrl(serviceUrl);
                if (turnContext.TurnState.Get<IConnectorClient>() == null)
                {
                    turnContext.TurnState.Add<IConnectorClient>(connectorClient);
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }

        // Gets the application credentials. App Credentials are cached so as to ensure we are not refreshing
        // token every time.
        // </summary>

        /// <param name="appId">The application identifier (AAD Id for the bot).</param>
        /// <returns>App credentials.</returns>
        private async Task<MicrosoftAppCredentials> GetAppCredentialsAsync(string appId)
        {
            if (appId == null)
            {
                return MicrosoftAppCredentials.Empty;
            }

            bool isAppCredentialsCached = this.appCredentialMap.TryGetValue(appId, out MicrosoftAppCredentials appCredentials);
            if (!isAppCredentialsCached || string.IsNullOrEmpty(appCredentials.MicrosoftAppPassword))
            {
                string appPassword = await this.credentialProvider.GetAppPasswordAsync(appId).ConfigureAwait(false);
                appCredentials = new MicrosoftAppCredentials(appId, appPassword);
                this.appCredentialMap[appId] = appCredentials;
            }

            return appCredentials;
        }
    }
}
