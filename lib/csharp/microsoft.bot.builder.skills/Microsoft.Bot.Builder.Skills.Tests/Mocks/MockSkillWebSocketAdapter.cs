using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Skills.Tests.Mocks
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
