using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;

namespace ServiceAdapter
{
    /// <summary>
    /// ServiceAdapter is an adapter that handles requests from external service directly
    /// It bypasses the channels BotFramework supports natively so it needs to take care of
    /// some of the channel responsibilities, such as authenticate, throttle, payload conversation etc
    /// Implementation of the service adapter should be in charge of detailed implementation of these.
    /// </summary>
    public abstract class ServiceAdapter : BotAdapter, IServiceAdapter, IUserTokenProvider
    {
        private EndpointService _endpointService;

        protected string ChannelId { get; set; }

        public ServiceAdapter(string channelId, EndpointService endpointService)
        {
            ChannelId = channelId;
            _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
        }

        /// <summary>
        /// ProcessAsync implementation contains five parts:
        /// 1. Authenticate
        /// 2. Throttle
        /// 3. Payload conversation
        /// 4. Activity management
        /// 5. RunPipeline (middleware, dialog etc)
        /// </summary>
        /// <param name="httpRequest">http request.</param>
        /// <param name="callback">callback in the bot.</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>task.</returns>
        public async Task ProcessAsync(HttpRequest httpRequest, BotCallbackHandler callback, CancellationToken cancellationToken)
        {
            // authenticate the request
            await Authenticate(httpRequest);

            // throttle the request
            await Throttle(httpRequest);

            // extract the activity from the request
            var activity = GetActivity(httpRequest);

            BotAssert.ActivityNotNull(activity);

            // verify if from.id is not null
            if (string.IsNullOrWhiteSpace(activity.From?.Id))
            {
                throw new ArgumentNullException("Activity must have a From.Id as user id!");
            }

            // create activity id if there isn't one
            if (string.IsNullOrWhiteSpace(activity.Id))
            {
                activity.Id = Guid.NewGuid().ToString();
            }

            // set activity type to message if it's empty
            if (string.IsNullOrWhiteSpace(activity.Type))
            {
                activity.Type = ActivityTypes.Message;
            }

            // create conversation id if there isn't one
            if (string.IsNullOrWhiteSpace(activity.Conversation?.Id))
            {
                if (activity.Conversation == null)
                {
                    activity.Conversation = new ConversationAccount();
                }

                activity.Conversation.Id = Guid.NewGuid().ToString();
            }

            // create recipient object if empty
            if (string.IsNullOrWhiteSpace(activity.Recipient?.Id))
            {
                if (activity.Recipient == null)
                {
                    activity.Recipient = new ChannelAccount();
                }

                activity.Recipient.Id = _endpointService.AppId;
                activity.Recipient.Name = "Bot";
                activity.Recipient.Role = "Bot";
            }

            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, callback, cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task Authenticate(HttpRequest httpRequest);

        protected abstract Task Throttle(HttpRequest httpRequest);

        protected abstract Activity GetActivity(HttpRequest httpRequest);

        /// <summary>Attempts to retrieve the token for a user that's in a login flow.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation with the user.</param>
        /// <param name="connectionName">Name of the auth connection to use.</param>
        /// <param name="magicCode">(Optional) Optional user entered code to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Token Response.</returns>
        public virtual async Task<TokenResponse> GetUserTokenAsync(ITurnContext turnContext, string connectionName, string magicCode, CancellationToken cancellationToken)
        {
            BotAssert.ContextNotNull(turnContext);
            if (turnContext.Activity.From == null || string.IsNullOrWhiteSpace(turnContext.Activity.From.Id))
            {
                throw new ArgumentNullException("BotFrameworkAdapter.GetuserToken(): missing from or from.id");
            }

            if (string.IsNullOrWhiteSpace(connectionName))
            {
                throw new ArgumentNullException(nameof(connectionName));
            }

            var client = CreateOAuthApiClientAsync(turnContext);
            return await client.UserToken.GetTokenAsync(turnContext.Activity.From.Id, connectionName, turnContext.Activity.ChannelId, magicCode, cancellationToken).ConfigureAwait(false);
        }

        public Task<string> GetOauthSignInLinkAsync(ITurnContext turnContext, string connectionName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetOauthSignInLinkAsync(ITurnContext turnContext, string connectionName, string userId, string finalRedirect = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task SignOutUserAsync(ITurnContext turnContext, string connectionName = null, string userId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<TokenStatus[]> GetTokenStatusAsync(ITurnContext context, string userId, string includeFilter = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, TokenResponse>> GetAadTokensAsync(ITurnContext context, string connectionName, string[] resourceUrls, string userId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an OAuth client for the bot.
        /// </summary>
        /// <param name="turnContext">The context object for the current turn.</param>
        /// <returns>An OAuth client for the bot.</returns>
        protected virtual OAuthClient CreateOAuthApiClientAsync(ITurnContext turnContext)
        {
            return new OAuthClient(new Uri(OAuthClientConfig.OAuthEndpoint), new MicrosoftAppCredentials(_endpointService.AppId, _endpointService.AppPassword));
        }
    }
}