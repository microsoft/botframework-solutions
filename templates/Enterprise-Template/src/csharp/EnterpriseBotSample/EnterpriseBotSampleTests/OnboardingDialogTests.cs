using EnterpriseBotSample.Dialogs.Onboarding;
using EnterpriseBotSample.Dialogs.Onboarding.Resources;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace EnterpriseBotSampleTests
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
                .AssertReply(OnboardingStrings.EMAIL_PROMPT)
                .Send(testEmail)
                .AssertReply(string.Format(OnboardingStrings.HAVE_EMAIL, testEmail))
                .AssertReply(OnboardingStrings.LOCATION_PROMPT)
                .Send(testLocation)
                .AssertReply(string.Format(OnboardingStrings.HAVE_LOCATION, testName, testLocation))
                .StartTestAsync();
        }
    }
}
