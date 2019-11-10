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
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
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
    public class SkillWebSocketAdapter : SkillWebSocketAdapterBase
    {
        public SkillWebSocketAdapter(
            SkillWebSocketBotAdapter skillWebSocketBotAdapter,
            BotSettingsBase botSettingsBase,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider,
            IBotTelemetryClient botTelemetryClient = null)
            : base(skillWebSocketBotAdapter, new MsJWTAuthenticator(botSettingsBase, whitelistAuthenticationProvider), botTelemetryClient)
        {
        }
    }
}