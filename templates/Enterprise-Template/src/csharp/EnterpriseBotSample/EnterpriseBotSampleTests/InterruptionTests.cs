using EnterpriseBotSample.Dialogs.Cancel.Resources;
using EnterpriseBotSample.Dialogs.Onboarding.Resources;
using EnterpriseBotSampleTests.Utterances;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace EnterpriseBotSampleTests
{
    [TestClass]
    public class InterruptionTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            await GetTestFlow()
               .Send(new Activity()
               {
                   ChannelId = Channels.Emulator,
                   Type = ActivityTypes.Event,
                   Value = new JObject(new JProperty("action", "startOnboarding"))
               })
               .AssertReply(OnboardingStrings.NAME_PROMPT)
               .Send(GeneralUtterances.Help)
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReply(OnboardingStrings.NAME_PROMPT)
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption_Confirmed()
        {
            await GetTestFlow()
               .Send(new Activity()
               {
                   Type = ActivityTypes.Event,
                   Value = new JObject(new JProperty("action", "startOnboarding"))
               })
               .AssertReply(OnboardingStrings.NAME_PROMPT)
               .Send(GeneralUtterances.Cancel)
               .AssertReply(activity => Assert.IsTrue(activity.AsMessageActivity().Text.Contains(CancelStrings.CANCEL_PROMPT)))
               .Send(GeneralUtterances.Confirm)
               .AssertReply(CancelStrings.CANCEL_CONFIRMED)
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption_Rejected()
        {
            await GetTestFlow()
               .Send(new Activity()
               {
                   Type = ActivityTypes.Event,
                   Value = new JObject(new JProperty("action", "startOnboarding"))
               })
               .AssertReply(OnboardingStrings.NAME_PROMPT)
               .Send(GeneralUtterances.Cancel)
               .AssertReply(activity => Assert.IsTrue(activity.AsMessageActivity().Text.Contains(CancelStrings.CANCEL_PROMPT)))
               .Send(GeneralUtterances.Reject)
               .AssertReply(CancelStrings.CANCEL_DENIED)
               .StartTestAsync();
        }
    }
}
