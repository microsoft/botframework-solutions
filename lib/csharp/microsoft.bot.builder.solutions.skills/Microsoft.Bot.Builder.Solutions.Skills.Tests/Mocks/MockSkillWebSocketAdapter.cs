using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Builder.Solutions.Skills.ToBeDeleted;

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
