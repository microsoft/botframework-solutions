// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.Tests
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
               .AssertReply(TemplateEngine.EvaluateTemplate("namePrompt"))
               .Send(GeneralUtterances.Help)
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReply(TemplateEngine.EvaluateTemplate("namePrompt"))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            await GetTestFlow()
               .Send(GeneralUtterances.Cancel)
               .AssertReply(TemplateEngine.EvaluateTemplate("nothingToCancelMessage"))
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
               .AssertReply(TemplateEngine.EvaluateTemplate("namePrompt"))
               .Send(GeneralUtterances.Cancel)
               .AssertReply((activity) =>
               {
                   var message = activity.AsMessageActivity();
                   var template = TemplateEngine.EvaluateTemplate("cancelPrompt");
                   Assert.IsTrue(message.Text.Contains(template));
                })
               .Send(GeneralUtterances.Confirm)
               .AssertReply(TemplateEngine.EvaluateTemplate("cancelConfirmedMessage"))
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
               .AssertReply(TemplateEngine.EvaluateTemplate("namePrompt"))
               .Send(GeneralUtterances.Cancel)
                .AssertReply((activity) =>
                {
                    var message = activity.AsMessageActivity();
                    var template = TemplateEngine.EvaluateTemplate("cancelPrompt");
                    Assert.IsTrue(message.Text.Contains(template));
                })
               .Send(GeneralUtterances.Reject)
               .AssertReply(TemplateEngine.EvaluateTemplate("cancelDeniedMessage"))
               .StartTestAsync();
        }
    }
}
