// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Bot.Builder.StreamingExtensions
{
    /// <summary>
    /// An adapter meant for subclassing and injecting custom implementation of BotFrameworkHttpAdapter for handling Http requests.
    /// </summary>
    public class WebSocketEnabledHttpAdapter : BotAdapter, IBotFrameworkHttpAdapter
    {
        private readonly BotFrameworkHttpAdapter _botFrameworkHttpAdapter;
        private readonly WebSocketConnector _webSocketConnector;
        private readonly object initLock = new object();
        private readonly List<Builder.IMiddleware> middlewares = new List<Builder.IMiddleware>();
        private Lazy<bool> _ensureMiddlewareSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketEnabledHttpAdapter"/> class.
        /// An adapter meant for subclassing and injecting custom implementation of BotFrameworkHttpAdapter for handling Http requests.
        /// Throws <see cref="ArgumentNullException"/> if configure is null.
        /// </summary>
        /// <param name="configuration">The configuration for the adapter to use.</param>
        /// <param name="credentialProvider">Optional credential provider.</param>
        /// <param name="channelProvider">Optional channel provider.</param>
        /// <param name="loggerFactory">Optional logger factory.</param>
        public WebSocketEnabledHttpAdapter(IConfiguration configuration, ICredentialProvider credentialProvider = null, IChannelProvider channelProvider = null, ILoggerFactory loggerFactory = null)
            : this(configuration, null, credentialProvider ?? new ConfigurationCredentialProvider(configuration), channelProvider ?? new ConfigurationChannelProvider(configuration), loggerFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketEnabledHttpAdapter"/> class.
        /// An adapter meant for subclassing and injecting custom implementation of BotFrameworkHttpAdapter for handling Http requests.
        /// Throws <see cref="ArgumentNullException"/> if configure is null.
        /// </summary>
        /// <param name="configuration">The configuration for the adapter to use.</param>
        /// <param name="botFrameworkHttpAdapter">Optional http adapter to use for non-streaming extensions requests.</param>
        /// <param name="credentialProvider">Optional credential provider.</param>
        /// <param name="channelProvider">Optional channel provider.</param>
        /// <param name="loggerFactory">Optional logger factory.</param>
        protected WebSocketEnabledHttpAdapter(IConfiguration configuration, IBotFrameworkHttpAdapter botFrameworkHttpAdapter = null, ICredentialProvider credentialProvider = null, IChannelProvider channelProvider = null, ILoggerFactory loggerFactory = null)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var openIdEndpoint = configuration.GetSection(AuthenticationConstants.BotOpenIdMetadataKey)?.Value;

            if (!string.IsNullOrEmpty(openIdEndpoint))
            {
                // If an open ID endpoint is configured, use it. This enables Public and Sovereign clouds that require open id.
                ChannelValidation.OpenIdMetadataUrl = openIdEndpoint;
                GovernmentChannelValidation.OpenIdMetadataUrl = openIdEndpoint;
            }

            credentialProvider = credentialProvider ?? new ConfigurationCredentialProvider(configuration);
            channelProvider = channelProvider ?? new ConfigurationChannelProvider(configuration);

            _botFrameworkHttpAdapter = _botFrameworkHttpAdapter ?? new BotFrameworkHttpAdapter(credentialProvider, channelProvider, loggerFactory?.CreateLogger<BotFrameworkHttpAdapter>());
            _webSocketConnector = new WebSocketConnector(credentialProvider, channelProvider);

            _ensureMiddlewareSet = new Lazy<bool>(() =>
            {
                middlewares.ForEach(mw => _botFrameworkHttpAdapter.Use(mw));
                _botFrameworkHttpAdapter.OnTurnError = OnTurnError;
                return true;
            });
        }

        /// <summary>
        /// Gets or sets the function to execute when the bot encounters a turn error.
        /// </summary>
        /// <value>
        /// The function to execute when the bot encounters a turn error.
        /// </value>
        public new Func<ITurnContext, Exception, Task> OnTurnError { get; set; }

        /// <summary>
        ///  In the case of a WebSocket upgrade request this method hands off to the registered Bot Framework Protocol v3 with Streaming Extensions compliant adapter.
        ///  In all other cases acts as a passthrough to the registered Bot Framework adapter.
        ///  Throws <see cref="ArgumentNullException"/> when required arguments are null.
        /// </summary>
        /// <param name="httpRequest">The request captured by the bot controller.</param>
        /// <param name="httpResponse">A response to be sent in answer to the request.</param>
        /// <param name="bot">The bot to use when processing the request.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException(nameof(httpRequest));
            }

            if (httpResponse == null)
            {
                throw new ArgumentNullException(nameof(httpResponse));
            }

            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            if (HttpMethods.IsGet(httpRequest.Method) && httpRequest.HttpContext.WebSockets.IsWebSocketRequest)
            {
                await _webSocketConnector.ProcessAsync(OnTurnError, middlewares, httpRequest, httpResponse, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bool check = _ensureMiddlewareSet.Value;
                await _botFrameworkHttpAdapter.ProcessAsync(httpRequest, httpResponse, bot, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Registers middleware with the adapter.
        /// Throws <see cref="ArgumentNullException"/> if middleware is null.
        /// </summary>
        /// <param name="middleware">The collection of middleware to execute when the adapter runs the pipeline.</param>
        /// <returns>This adapter.</returns>
        public new WebSocketEnabledHttpAdapter Use(Builder.IMiddleware middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            lock (initLock)
            {
                middlewares.Add(middleware);
            }

            return this;
        }

        /// <summary>
        /// Not implemented.
        /// Throws <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="turnContext">turnContext is ignored.</param>
        /// <param name="activities">activities is ignored.</param>
        /// <param name="cancellationToken">cancellationToken is ignored.</param>
        /// <returns>Nothing is returned.</returns>
        public override Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <summary>
        /// Not implemented.
        /// Throws <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="turnContext">turnContext is ignored.</param>
        /// <param name="activity">activity is ignored.</param>
        /// <param name="cancellationToken">cancellationToken is ignored.</param>
        /// <returns>Nothing is returned.</returns>
        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken) => throw new NotImplementedException();

        /// <summary>
        /// Not implemented.
        /// Throws <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="turnContext">turnContext is ignored.</param>
        /// <param name="reference">reference is ignored.</param>
        /// <param name="cancellationToken">cancellationToken is ignored.</param>
        /// <returns>Nothing is returned.</returns>
        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}
