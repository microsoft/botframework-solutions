// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
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
            var allNamePromptVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NamePrompt");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReplyOneOf(allNamePromptVariations.ToArray())
               .Send(GeneralUtterances.Help)
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReplyOneOf(allNamePromptVariations.ToArray())
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            var allResponseVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("CancelledMessage", TestUserProfileState);

            await GetTestFlow()
               .Send(GeneralUtterances.Cancel)
               .AssertReplyOneOf(allResponseVariations.ToArray())
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption_Confirmed()
        {
            var allNamePromptVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NamePrompt");
            var allCancelledVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("CancelledMessage", TestUserProfileState);

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReplyOneOf(allNamePromptVariations.ToArray())
               .Send(GeneralUtterances.Cancel)
               .AssertReplyOneOf(allCancelledVariations.ToArray())
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Repeat_Interruption()
        {
            var allNamePromptVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NamePrompt");

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReplyOneOf(allNamePromptVariations.ToArray())
               .Send(GeneralUtterances.Repeat)
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReplyOneOf(allNamePromptVariations.ToArray())
               .StartTestAsync();
        }
    }
}
