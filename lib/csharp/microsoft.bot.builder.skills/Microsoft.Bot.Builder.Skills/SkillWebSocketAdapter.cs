using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.StreamingExtensions.Transport;
using Microsoft.Bot.StreamingExtensions.Transport.WebSockets;

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
        private readonly IAuthenticator _authenticator;
        private readonly Stopwatch _stopWatch;

        public SkillWebSocketAdapter(
            SkillWebSocketBotAdapter skillWebSocketBotAdapter,
            BotSettingsBase botSettingsBase,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider,
            IBotTelemetryClient botTelemetryClient = null)
        {
            _skillWebSocketBotAdapter = skillWebSocketBotAdapter ?? throw new ArgumentNullException(nameof(skillWebSocketBotAdapter));
            if (botSettingsBase == null)
            {
                throw new ArgumentNullException(nameof(botSettingsBase));
            }

            if (whitelistAuthenticationProvider == null)
            {
                throw new ArgumentNullException(nameof(whitelistAuthenticationProvider));
            }

            var authenticationProvider = new MSJwtAuthenticationProvider(botSettingsBase.MicrosoftAppId);
            _authenticator = new Authenticator(authenticationProvider, whitelistAuthenticationProvider);

            _botTelemetryClient = botTelemetryClient ?? NullBotTelemetryClient.Instance;
            _stopWatch = new Stopwatch();
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
                httpResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                await httpResponse.WriteAsync("Upgrade to WebSocket required.", cancellationToken: cancellationToken).ConfigureAwait(false);
                return;
            }

            var claimsIdentity = await _authenticator.Authenticate(httpRequest, httpResponse).ConfigureAwait(false);

            await CreateWebSocketConnectionAsync(claimsIdentity, httpRequest.HttpContext, bot).ConfigureAwait(false);
        }

        private async Task CreateWebSocketConnectionAsync(ClaimsIdentity claimsIdentity, HttpContext httpContext, IBot bot)
        {
            var socket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var handler = new SkillWebSocketRequestHandler(claimsIdentity, _botTelemetryClient);
            var server = new WebSocketServer(socket, handler);
            server.Disconnected += Server_Disconnected;
            _skillWebSocketBotAdapter.Server = server;
            handler.Bot = bot;
            handler.SkillWebSocketBotAdapter = _skillWebSocketBotAdapter;

            _botTelemetryClient.TrackTrace("Starting listening on websocket", Severity.Information, null);
            _stopWatch.Start();
            var startListening = server.StartAsync();
            Task.WaitAll(startListening);
        }

        private void Server_Disconnected(object sender, DisconnectedEventArgs e)
        {
            if (_stopWatch.IsRunning)
            {
                _stopWatch.Stop();

                _botTelemetryClient.TrackEvent("SkillWebSocketOpenCloseLatency", null, new Dictionary<string, double>
                {
                    { "Latency", _stopWatch.ElapsedMilliseconds },
                });
            }
        }
    }
}
