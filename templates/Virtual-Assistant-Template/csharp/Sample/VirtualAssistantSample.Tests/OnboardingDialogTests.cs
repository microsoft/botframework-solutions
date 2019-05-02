using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Responses.Onboarding;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    public class OnboardingDialogTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Onboarding_Flow()
        {
            var testName = "Jane Doe";

            await GetTestFlow()
                .Send(new Activity()
                {
                    ChannelId = Channels.Emulator,
                    Type = ActivityTypes.Event,
                    Value = new JObject(new JProperty("action", "startOnboarding"))
                })
                .AssertReply(OnboardingStrings.NAME_PROMPT)
                .Send(testName)
                .AssertReply(string.Format(OnboardingStrings.HAVE_NAME, testName))
                .StartTestAsync();
        }
    }
}
