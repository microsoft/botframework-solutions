using System.Net.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillController : SkillController
    {
        public MockSkillController(
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            SkillHttpAdapter skillHttpAdapter,
            SkillWebSocketAdapter skillWebSocketAdapter,
            IBot bot,
            BotSettingsBase botSettings,
			HttpClient httpClient,
            string manifestFileOverride = null)
            : base(botFrameworkHttpAdapter, skillHttpAdapter, skillWebSocketAdapter, bot, botSettings)
        {
			HttpClient = httpClient;

            if (manifestFileOverride != null)
            {
                ManifestTemplateFilename = manifestFileOverride;
            }
        }
    }
}