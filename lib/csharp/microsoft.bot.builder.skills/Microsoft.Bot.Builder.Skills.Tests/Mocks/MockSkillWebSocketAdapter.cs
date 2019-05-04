namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillWebSocketAdapter : SkillWebSocketAdapter
    {
        public MockSkillWebSocketAdapter(SkillWebSocketBotAdapter skillWebSocketBotAdapter)
            : base(skillWebSocketBotAdapter)
        {
        }
    }
}