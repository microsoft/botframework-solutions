// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Solutions;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillController : SkillController
    {
        public MockSkillController(
            IBot bot,
            BotSettingsBase botSettings,
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            SkillWebSocketAdapter skillWebSocketAdapter,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider,
            HttpClient httpClient,
            string manifestFileOverride = null)
            : base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter, whitelistAuthenticationProvider)
        {
			HttpClient = httpClient;

            if (manifestFileOverride != null)
            {
                ManifestTemplateFilename = manifestFileOverride;
            }
        }
    }
}