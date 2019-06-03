// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using AdaptiveAssistant.Responses.Cancel;
using AdaptiveAssistant.Responses.Onboarding;
using AdaptiveAssistant.Tests.Utterances;

namespace AdaptiveAssistant.Tests
{
    [TestClass]
    public class InterruptionTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            await GetTestFlow()
               .Send(GeneralUtterances.Help)
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help_Interruption_In_Dialog()
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
        public async Task Test_Cancel_Interruption()
        {
            await GetTestFlow()
               .Send(GeneralUtterances.Cancel)
               .AssertReply(activity =>
               {
                   Assert.IsTrue(activity.AsMessageActivity().Text.Contains(CancelStrings.NOTHING_TO_CANCEL));
               })
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
