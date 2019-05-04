using System.Net.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Solutions;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockSkillController : SkillController
    {
        public MockSkillController(
            IBot bot,
            BotSettingsBase botSettings,
            IBotFrameworkHttpAdapter botFrameworkHttpAdapter,
            SkillWebSocketAdapter skillWebSocketAdapter,
            HttpClient httpClient,
            string manifestFileOverride = null)
            : base(bot, botSettings, botFrameworkHttpAdapter, skillWebSocketAdapter)
        {
			HttpClient = httpClient;

            if (manifestFileOverride != null)
            {
                ManifestTemplateFilename = manifestFileOverride;
            }
        }
    }
}