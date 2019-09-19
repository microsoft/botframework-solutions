// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.StreamingExtensions;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Bot.StreamingExtensions.Transport.NamedPipes;
using Microsoft.Bot.StreamingExtensions.Transport.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Bot.Builder.StreamingExtensions
{
#pragma warning disable SA1202
#pragma warning disable SA1401
#pragma warning disable IDE0034
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
                              /// <summary>
                              /// Used to process incoming requests sent over an <see cref="IStreamingTransport"/> and adhering to the Bot Framework Protocol v3 with Streaming Extensions.
                              /// </summary>
    public class StreamingRequestHandler : RequestHandler
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
    {
        private readonly IBot _bot;

        private readonly IList<IMiddleware> _middlewareSet;

        private readonly Func<ITurnContext, Exception, Task> _onTurnError;

        private readonly IServiceProvider _services;

#if DEBUG
        public
#else
        private
#endif
        IStreamingTransportServer _transportServer;

#pragma warning disable IDE0044
#if DEBUG
        public
#else
        private
#endif
        string _userAgent;
#pragma warning restore IDE0044

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingRequestHandler"/> class.
        /// The StreamingRequestHandler serves as a translation layer between the transport layer and bot adapter.
        /// It receives ReceiveRequests from the transport and provides them to the bot adapter in a form
        /// it is able to build activities out of, which are then handed to the bot itself to processed.
        /// Throws <see cref="ArgumentNullException"/> if arguments are null.
        /// </summary>
        /// <param name="onTurnError">Optional function to perform on turn errors.</param>
        /// <param name="bot">The <see cref="IBot"/> to be used for all requests to this handler.</param>
        /// <param name="middlewareSet">An optional set of middleware to register with the bot.</param>
        public StreamingRequestHandler(Func<ITurnContext, Exception, Task> onTurnError, IBot bot, IList<IMiddleware> middlewareSet = null)
        {
            _onTurnError = onTurnError;
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _middlewareSet = middlewareSet ?? new List<IMiddleware>();
            _userAgent = GetUserAgent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamingRequestHandler"/> class.
        /// An overload for use with dependency injection via ServiceProvider, as shown
        /// in DotNet Core Bot Builder samples.
        /// Throws <see cref="ArgumentNullException"/> if arguments are null.
        /// </summary>
        /// <param name="onTurnError">Optional function to perform on turn errors.</param>
        /// <param name="serviceProvider">The service collection containing the registered IBot type.</param>
        /// <param name="middlewareSet">An optional set of middleware to register with the bot.</param>
        public StreamingRequestHandler(Func<ITurnContext, Exception, Task> onTurnError, IServiceProvider serviceProvider, IList<IMiddleware> middlewareSet = null)
        {
            _onTurnError = onTurnError;
            _services = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _middlewareSet = middlewareSet ?? new List<IMiddleware>();
            _userAgent = GetUserAgent();
        }

        /// <summary>
        /// Connects the handler to a WebSocket server and begins listening for incoming requests.
        /// </summary>
        /// <param name="socket">The socket to use when creating the server.</param>
        /// <returns>A task that runs until the server is disconnected.</returns>
        public Task StartAsync(WebSocket socket)
        {
            _transportServer = new WebSocketServer(socket, this);

            return _transportServer.StartAsync();
        }

        /// <summary>
        /// Connects the handler to a Named Pipe server and begins listening for incoming requests.
        /// </summary>
        /// <param name="pipeName">The name of the named pipe to use when creating the server.</param>
        /// <returns>A task that runs until the server is disconnected.</returns>
        public Task StartAsync(string pipeName)
        {
            _transportServer = new NamedPipeServer(pipeName, this);

            return _transportServer.StartAsync();
        }

        /// <summary>
        /// Checks the validity of the request and attempts to map it the correct virtual endpoint,
        /// then generates and returns a response if appropriate.
        /// </summary>
        /// <param name="request">A ReceiveRequest from the connected channel.</param>
        /// <param name="logger">Optional logger used to log request information and error details.</param>
        /// <param name="context">Optional context to operate within. Unused in bot implementation.</param>
        /// /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A response created by the BotAdapter to be sent to the client that originated the request.</returns>
        public override async Task<StreamingResponse> ProcessRequestAsync(ReceiveRequest request, ILogger<RequestHandler> logger, object context = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            logger = logger ?? NullLogger<RequestHandler>.Instance;
            var response = new StreamingResponse();

            if (request == null || string.IsNullOrEmpty(request.Verb) || string.IsNullOrEmpty(request.Path))
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                logger.LogError("Request missing verb and/or path.");

                return response;
            }

            if (string.Equals(request.Verb, StreamingRequest.GET, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(request.Path, "/api/version", StringComparison.InvariantCultureIgnoreCase))
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.SetBody(new VersionInfo() { UserAgent = _userAgent });

                return response;
            }

            if (string.Equals(request.Verb, StreamingRequest.POST, StringComparison.InvariantCultureIgnoreCase) &&
                         string.Equals(request.Path, "/api/messages", StringComparison.InvariantCultureIgnoreCase))
            {
                return await ProcessStreamingRequestAsync(request, response, logger, cancellationToken).ConfigureAwait(false);
            }

            response.StatusCode = (int)HttpStatusCode.NotFound;
            logger.LogError($"Unknown verb and path: {request.Verb} {request.Path}");

            return response;
        }

        /// <summary>
        /// Build and return versioning information used for telemetry, including:
        /// The Schema version is 3.1, put into the Microsoft-BotFramework header,
        /// Protocol Extension Info,
        /// The Client SDK Version
        ///  https://github.com/Microsoft/botbuilder-dotnet/blob/d342cd66d159a023ac435aec0fdf791f93118f5f/doc/UserAgents.md,
        /// Additional Info.
        /// https://github.com/Microsoft/botbuilder-dotnet/blob/d342cd66d159a023ac435aec0fdf791f93118f5f/doc/UserAgents.md.
        /// </summary>
        /// <returns>A string containing versioning information.</returns>
        private static string GetUserAgent() =>
            string.Format(
                "Microsoft-BotFramework/3.1 Streaming-Extensions/1.0 BotBuilder/{0} ({1}; {2}; {3})",
                ConnectorClient.GetClientVersion(new ConnectorClient(new Uri("http://localhost"))),
                ConnectorClient.GetASPNetVersion(),
                ConnectorClient.GetOsVersion(),
                ConnectorClient.GetArchitecture());

        /// <summary>
        /// Performs the actual processing of a request, handing it off to the adapter and returning the response.
        /// </summary>
        /// <param name="request">A ReceiveRequest from the connected channel.</param>
        /// <param name="response">The response to update and return, ultimately sent to client.</param>
        /// <param name="logger">Optional logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The response ready to send to the client.</returns>
        private async Task<StreamingResponse> ProcessStreamingRequestAsync(ReceiveRequest request, StreamingResponse response, ILogger<RequestHandler> logger, CancellationToken cancellationToken)
        {
            var body = string.Empty;

            try
            {
                body = request.ReadBodyAsString();
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                logger.LogError("Request body missing or malformed: " + ex.Message);

                return response;
            }

            try
            {
                var adapter = new BotFrameworkStreamingExtensionsAdapter(_transportServer, _middlewareSet, logger);
                IBot bot = null;

                // First check if an IBot type definition is available from the service provider.
                if (_services != null)
                {
                    /* Creating a new scope for each request allows us to support
                     * bots that inject scoped services as dependencies.
                     */
                    bot = _services.CreateScope().ServiceProvider.GetService<IBot>();
                }

                // If no bot has been set, check if a singleton bot is associated with this request handler.
                if (bot == null)
                {
                    bot = _bot;
                }

                // If a bot still hasn't been set, the request will not be handled correctly, so throw and terminate.
                if (bot == null)
                {
                    throw new Exception("Unable to find bot when processing request.");
                }

                adapter.OnTurnError = _onTurnError;
                var invokeResponse = await adapter.ProcessActivityAsync(body, request.Streams, new BotCallbackHandler(bot.OnTurnAsync), cancellationToken).ConfigureAwait(false);

                if (invokeResponse == null)
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    response.StatusCode = invokeResponse.Status;
                    if (invokeResponse.Body != null)
                    {
                        response.SetBody(invokeResponse.Body);
                    }
                }

                invokeResponse = null;
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                logger.LogError(ex.Message);
            }

            return response;
        }
    }
}
