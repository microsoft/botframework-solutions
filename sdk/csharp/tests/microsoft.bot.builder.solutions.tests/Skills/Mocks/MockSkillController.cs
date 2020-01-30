// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills.Mocks
{
    [Obsolete("This type is being deprecated.", false)]
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