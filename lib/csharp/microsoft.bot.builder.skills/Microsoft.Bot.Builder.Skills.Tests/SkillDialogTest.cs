using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    // Extended implementation of SkillDialog for test purposes that enables us to mock the HttpClient
    internal class SkillDialogTest : SkillDialog
    {
        public SkillDialogTest(SkillManifest skillManifest, IServiceClientCredentials serviceClientCredentials, IBotTelemetryClient telemetryClient, UserState userState, ISkillTransport skillTransport = null)
            : base(skillManifest, serviceClientCredentials, telemetryClient, userState, null, skillTransport)
        {
        }
    }
}