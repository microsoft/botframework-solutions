namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillHttpBotAdapter : SkillHttpBotAdapter
    {
        public MockSkillHttpBotAdapter(IBotTelemetryClient botTelemetryClient = null)
            : base(botTelemetryClient)
        {
        }
    }
}