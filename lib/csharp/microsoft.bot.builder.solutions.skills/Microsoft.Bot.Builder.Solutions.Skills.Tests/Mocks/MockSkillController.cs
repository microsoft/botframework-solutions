using System.Net.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;

namespace Microsoft.Bot.Builder.Solutions.Skills.Tests.Mocks
{
    public class MockSkillController : SkillController
    {
        public MockSkillController(
            IBot bot,
            BotSettingsBase botSettings,
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            IWhitelistAuthenticationProvider whitelistAuthenticationProvider,
            HttpClient httpClient,
            string manifestFileOverride = null)
            : base(bot, botSettings, botFrameworkHttpAdapter, whitelistAuthenticationProvider)
        {
			HttpClient = httpClient;

            if (manifestFileOverride != null)
            {
                ManifestTemplateFilename = manifestFileOverride;
            }
        }
    }
}
