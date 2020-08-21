using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Extensions
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class ActivityExTests
    {
        [TestMethod]
        public void Test_ActivityExIsStartActivityTrue()
        {
            // Create MessageActivity
            var messageActivity = new Activity()
            {
                ChannelId = Connector.Channels.Test,
                Type = ActivityTypes.ConversationUpdate,
                MembersAdded = new List<ChannelAccount> { new ChannelAccount { Id = "Test" } },
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: $"Notification.TestProactive", name: $"Notification.Proactive"),
                Recipient = new ChannelAccount { Id = "Test" },
            };

            bool isStartActivity = messageActivity.IsStartActivity();
            Assert.IsTrue(isStartActivity);
        }

        [TestMethod]
        public void Test_ActivityExIsStartActivityFalse()
        {
            // Create MessageActivity
            var messageActivity = new Activity()
            {
                ChannelId = Connector.Channels.Test,
                Type = ActivityTypes.ConversationUpdate,
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: $"Notification.TestProactive", name: $"Notification.Proactive"),
                Recipient = new ChannelAccount { Id = "Test" },
            };

            bool isStartActivity = messageActivity.IsStartActivity();
            Assert.IsFalse(isStartActivity);
        }
    }
}
