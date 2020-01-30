// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills.Mocks
{
    [Obsolete("This type is being deprecated.", false)]
    public class MockSkillWebSocketAdapter : SkillWebSocketAdapter
    {
        public MockSkillWebSocketAdapter(
            SkillWebSocketBotAdapter skillWebSocketBotAdapter,
            BotSettingsBase botSettingsBase,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider)
            : base(skillWebSocketBotAdapter, botSettingsBase, whitelistAuthenticationProvider)
        {
        }
    }
}