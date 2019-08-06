using Microsoft.Bot.Builder.Solutions;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillWebSocketAdapter : SkillWebSocketAdapter
    {
        public MockSkillWebSocketAdapter(SkillWebSocketBotAdapter skillWebSocketBotAdapter, BotSettingsBase botSettingsBase)
            : base(skillWebSocketBotAdapter, botSettingsBase)
        {
        }
    }
}