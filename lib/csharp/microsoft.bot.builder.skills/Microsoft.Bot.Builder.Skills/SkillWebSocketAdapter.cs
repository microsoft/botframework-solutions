using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Protocol.WebSockets;

namespace Microsoft.Bot.Builder.Skills
{
    /// <summary>
    /// This adapter is responsible for accepting a bot-to-bot call over websocket transport.
    /// It'll perform the following tasks:
    /// 1. Authentication.
    /// 2. Create RequestHandler to handle follow-up websocket frames.
    /// 3. Start listening on the websocket connection.
    /// </summary>
    public class SkillWebSocketAdapter : IBotFrameworkHttpAdapter
    {
        private readonly IBotTelemetryClient _botTelemetryClient;
        private readonly SkillWebSocketBotAdapter _skillWebSocketBotAdapter;
        private readonly IAuthenticationProvider _authenticationProvider;

        public SkillWebSocketAdapter(
            SkillWebSocketBotAdapter skillWebSocketBotAdapter,
            IAuthenticationProvider authenticationProvider = null,
            IBotTelemetryClient botTelemetryClient = null)
        {
            _skillWebSocketBotAdapter = skillWebSocketBotAdapter ?? throw new ArgumentNullException(nameof(SkillWebSocketBotAdapter));
            _authenticationProvider = authenticationProvider;
            _botTelemetryClient = botTelemetryClient ?? NullBotTelemetryClient.Instance;
        }

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

            if (!httpRequest.HttpContext.WebSockets.IsWebSocketRequest)
            {
                httpRequest.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await httpRequest.HttpContext.Response.WriteAsync("Upgrade to WebSocket required.").ConfigureAwait(false);
                return;
            }

            if (_authenticationProvider != null)
            {
                var authenticated = _authenticationProvider.Authenticate(httpRequest.Headers["Authorization"]);

                if (!authenticated)
                {
                    httpResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return;
                }
            }

            await CreateWebSocketConnectionAsync(httpRequest.HttpContext, bot).ConfigureAwait(false);
        }

        private async Task CreateWebSocketConnectionAsync(HttpContext httpContext, IBot bot)
        {
            var socket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var handler = new SkillWebSocketRequestHandler();
            var server = new WebSocketServer(socket, handler);
            _skillWebSocketBotAdapter.Server = server;
            handler.Bot = bot;
            handler.SkillWebSocketBotAdapter = _skillWebSocketBotAdapter;

            _botTelemetryClient.TrackTrace("Starting listening on websocket", Severity.Information, null);
            var startListening = server.StartAsync();
            Task.WaitAll(startListening);
        }
    }
}