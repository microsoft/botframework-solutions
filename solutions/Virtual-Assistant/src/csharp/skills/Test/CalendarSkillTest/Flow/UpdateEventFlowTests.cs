// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Flow
{
    using System;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using CalendarSkill.Dialogs.UpdateEvent.Resources;
    using Microsoft.Bot.Schema;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UpdateEventFlowTests : CalendarBotTestBase
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
                .Send("Update a meeting")
                .AssertReplyOneOf(this.CollectStartDateTimeMessage())
                .Send("test title")
                .AssertReplyOneOf(this.CollectNewStartDateTimeMessage())
                .Send("9pm")
                .AssertReply(this.AssertComfirmBeforeUpdatePrompt())
                .Send("Yes")
                .AssertReply(this.UpdatedMessage())
                .StartTestAsync();
        }

        private Action<IActivity> UpdatedMessage()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(UpdateEventResponses.EventUpdated.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private Action<IActivity> AssertComfirmBeforeUpdatePrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(UpdateEventResponses.ConfirmUpdate.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectStartDateTimeMessage()
        {
            return this.ParseReplies(UpdateEventResponses.NoUpdateStartTime.Replies, new StringDictionary());
        }

        private string[] CollectNewStartDateTimeMessage()
        {
            return this.ParseReplies(UpdateEventResponses.NoNewTime.Replies, new StringDictionary());
        }
    }
}
