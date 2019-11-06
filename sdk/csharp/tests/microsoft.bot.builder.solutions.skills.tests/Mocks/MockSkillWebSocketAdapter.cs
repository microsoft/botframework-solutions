// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Solutions;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
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