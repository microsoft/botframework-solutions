// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Flow
{
    using System;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using CalendarSkill.Dialogs.Summary.Resources;
    using Microsoft.Bot.Schema;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SummaryFlowTests : CalendarBotTestBase
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
                .Send("What are my meetings today")
                .AssertReplyOneOf(this.ShowSummaryMessage())
                .AssertReply(this.ShowEventList())
                .StartTestAsync();
        }

        private string[] ShowSummaryMessage()
        {
            return this.ParseReplies(SummaryResponses.ShowOneMeetingSummaryMessage.Replies, new StringDictionary() { { "Count", "1" } });
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
