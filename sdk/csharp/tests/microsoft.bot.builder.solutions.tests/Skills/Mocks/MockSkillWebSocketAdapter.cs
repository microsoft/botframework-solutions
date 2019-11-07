// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills.Mocks
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