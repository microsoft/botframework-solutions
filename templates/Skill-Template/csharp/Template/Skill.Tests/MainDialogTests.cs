﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using $safeprojectname$.Utterances;

namespace $safeprojectname$
{
    [TestClass]
    public class MainDialogTests : SkillTestBase
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
                .AssertReply(TemplateEngine.GenerateActivityForLocale("IntroText"))
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
        public async Task Test_Unhandled_Message()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.None)
                .AssertReplyOneOf(GetTemplates("UnsupportedText"))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Single_Turn()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.None)
                .AssertReplyOneOf(GetTemplates("UnsupportedText"))
                .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.Handoff, activity.Type); })
                .StartTestAsync();
        }
    }
}