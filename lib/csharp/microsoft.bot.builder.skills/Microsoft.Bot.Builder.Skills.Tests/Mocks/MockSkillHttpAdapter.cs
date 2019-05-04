using Microsoft.Bot.Builder.Skills.Auth;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillHttpAdapter : SkillHttpAdapter
    {
        public MockSkillHttpAdapter(
            SkillHttpBotAdapter skillHttpBotAdapter,
            IAuthenticationProvider authenticationProvider = null,
            IBotTelemetryClient botTelemetryClient = null)
            : base(skillHttpBotAdapter, authenticationProvider, botTelemetryClient)
        {
        }
    }
}