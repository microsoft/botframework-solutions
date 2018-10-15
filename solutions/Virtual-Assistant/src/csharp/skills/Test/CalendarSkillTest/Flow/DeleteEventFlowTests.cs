// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Flow
{
    using System;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using CalendarSkill.Dialogs.DeleteEvent.Resources;
    using Microsoft.Bot.Schema;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DeleteEventFlowTests : CalendarBotTestBase
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
                .Send("Delete a meeting")
                .AssertReplyOneOf(this.CollectStartDateTimeMessage())
                .Send("test title")
                .AssertReply(this.AssertComfirmBeforeDeletePrompt())
                .Send("Yes")
                .AssertReplyOneOf(this.DeletedMessage())
                .StartTestAsync();
        }

        private string[] DeletedMessage()
        {
            return this.ParseReplies(DeleteEventResponses.EventDeleted.Replies, new StringDictionary());
        }

        private Action<IActivity> AssertComfirmBeforeDeletePrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(DeleteEventResponses.ConfirmDelete.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectStartDateTimeMessage()
        {
            return this.ParseReplies(DeleteEventResponses.NoDeleteStartTime.Replies, new StringDictionary());
        }
    }
}
