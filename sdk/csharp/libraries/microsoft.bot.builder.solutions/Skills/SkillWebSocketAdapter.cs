// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Streaming.Transport;
using Microsoft.Bot.Streaming.Transport.WebSockets;

namespace Microsoft.Bot.Builder.Solutions.Skills
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
        private readonly BotSettingsBase _botSettingsBase;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly IWhitelistAuthenticationProvider _whitelistAuthenticationProvider;
        private readonly IAuthenticator _authenticator;
        private readonly ICredentialProvider _credentialProvider;
        private readonly Stopwatch _stopWatch;

        public SkillWebSocketAdapter(
            SkillWebSocketBotAdapter skillWebSocketBotAdapter,
            BotSettingsBase botSettingsBase,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider,
            IBotTelemetryClient botTelemetryClient = null,
            ICredentialProvider credentialProvider = null)
        {
            _skillWebSocketBotAdapter = skillWebSocketBotAdapter ?? throw new ArgumentNullException(nameof(skillWebSocketBotAdapter));
            _botSettingsBase = botSettingsBase ?? throw new ArgumentNullException(nameof(botSettingsBase));
            _whitelistAuthenticationProvider = whitelistAuthenticationProvider ?? throw new ArgumentNullException(nameof(whitelistAuthenticationProvider));

            _credentialProvider = credentialProvider;
            _authenticationProvider = new MsJWTAuthenticationProvider(_botSettingsBase.MicrosoftAppId);
            _authenticator = new Authenticator(_authenticationProvider, _whitelistAuthenticationProvider);

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
                await httpResponse.WriteAsync("Upgrade to WebSocket required.").ConfigureAwait(false);
                return;
            }

            ClaimsIdentity claims;

            // only perform auth when it's enabled
            if (_credentialProvider != null && !await _credentialProvider.IsAuthenticationDisabledAsync().ConfigureAwait(false))
            {
                claims = await _authenticator.AuthenticateAsync(httpRequest, httpResponse).ConfigureAwait(false);
            }
            else
            {
                claims = new ClaimsIdentity(new List<Claim>(), "anonymous");
            }

            await CreateWebSocketConnectionAsync(claims, httpRequest.HttpContext, bot).ConfigureAwait(false);
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