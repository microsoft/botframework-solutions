using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Responses;
using RichardSzalay.MockHttp;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    // Extended implementation of SkillDialog for test purposes that enables us to mock the HttpClient
    internal class SkillDialogTest : SkillDialog
    {
        private MockHttpMessageHandler _mockHttpMessageHandler;

        public SkillDialogTest(SkillManifest skillManifest, MicrosoftAppCredentialsEx microsoftAppCredentialsEx, IBotTelemetryClient telemetryClient, MockHttpMessageHandler mockHttpMessageHandler, UserState userState)
            : base(skillManifest, microsoftAppCredentialsEx, telemetryClient, userState)
        {
            _mockHttpMessageHandler = mockHttpMessageHandler;
        }
    }
}