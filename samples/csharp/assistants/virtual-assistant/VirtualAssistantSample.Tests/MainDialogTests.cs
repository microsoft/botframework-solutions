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
    public class MainDialogTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Intro_Message()
        {
            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help_Intent()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Help)
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Escalate_Intent()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Escalate)
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Unhandled_Message()
        {
            var allResponseVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("UnsupportedMessage", TestUserProfileState);

            await GetTestFlow()
                .Send("Unhandled message")
                .AssertReplyOneOf(allResponseVariations.ToArray())
                .StartTestAsync();
        }
    }
}