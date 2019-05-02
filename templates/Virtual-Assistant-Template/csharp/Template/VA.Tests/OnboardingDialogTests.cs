using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using $ext_safeprojectname$.Responses.Onboarding;

namespace $safeprojectname$
{
    [TestClass]
    public class OnboardingDialogTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Onboarding_Flow()
        {
            var testName = "Jane Doe";
            var testEmail = "jane@email.com";
            var testLocation = "Seattle, WA";

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
