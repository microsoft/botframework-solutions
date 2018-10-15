// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Flow
{
    using System;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using CalendarSkill.Dialogs.CreateEvent.Resources;
    using Microsoft.Bot.Schema;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CreateEventFlowTests : CalendarBotTestBase
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
                .Send("Create a meeting")
                .AssertReplyOneOf(this.CollectAttendeesMessage())
                .Send("TestName")
                .AssertReplyOneOf(this.CollectTitleMessage())
                .Send("TestTitle")
                .AssertReplyOneOf(this.CollectContentMessage())
                .Send("TestContent")
                .AssertReplyOneOf(this.CollectStartDateMessage())
                .Send("today")
                .AssertReplyOneOf(this.CollectStartTimeMessage())
                .Send("6pm")
                .AssertReplyOneOf(this.CollectDurationMessage())
                .Send("1 hour")
                .AssertReplyOneOf(this.CollectLocationMessage())
                .Send("TestLocation")
                .AssertReply(this.AssertComfirmBeforeCreatePrompt())
                .Send("Yes")
                .AssertReplyOneOf(this.CreatedMessage())
                .StartTestAsync();
        }

        private string[] CreatedMessage()
        {
            return this.ParseReplies(CreateEventResponses.EventCreated.Replies, new StringDictionary());
        }

        private Action<IActivity> AssertComfirmBeforeCreatePrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                CollectionAssert.Contains(this.ParseReplies(CreateEventResponses.ConfirmCreate.Replies, new StringDictionary()), messageActivity.Text);
                Assert.AreEqual(messageActivity.Attachments.Count, 1);
            };
        }

        private string[] CollectAttendeesMessage()
        {
            return this.ParseReplies(CreateEventResponses.NoAttendeesMS.Replies, new StringDictionary());
        }

        private string[] CollectTitleMessage()
        {
            return this.ParseReplies(CreateEventResponses.NoTitle.Replies, new StringDictionary() { { "UserName", "TestName" } });
        }

        private string[] CollectContentMessage()
        {
            return this.ParseReplies(CreateEventResponses.NoContent.Replies, new StringDictionary());
        }

        private string[] CollectStartDateMessage()
        {
            return this.ParseReplies(CreateEventResponses.NoStartDate.Replies, new StringDictionary());
        }

        private string[] CollectStartTimeMessage()
        {
            return this.ParseReplies(CreateEventResponses.NoStartTime.Replies, new StringDictionary());
        }

        private string[] CollectDurationMessage()
        {
            return this.ParseReplies(CreateEventResponses.NoDuration.Replies, new StringDictionary());
        }

        private string[] CollectLocationMessage()
        {
            return this.ParseReplies(CreateEventResponses.NoLocation.Replies, new StringDictionary());
        }
    }
}
