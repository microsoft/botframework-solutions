// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Flow
{
    using System;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using CalendarSkill.Dialogs.NextMeeting.Resources;
    using Microsoft.Bot.Schema;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class NextEventFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        [TestMethod]
        public async Task EndToEnd()
        {
            await this.GetTestFlow()
                .Send("What is my next meeting?")
                .AssertReplyOneOf(this.ShowNextEventMessage())
                .AssertReply(this.ShowEventList())
                .StartTestAsync();
        }

        private string[] ShowNextEventMessage()
        {
            return this.ParseReplies(NextMeetingResponses.ShowNextMeetingMessage.Replies, new StringDictionary());
        }

        private Action<IActivity> ShowEventList()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }
    }
}
