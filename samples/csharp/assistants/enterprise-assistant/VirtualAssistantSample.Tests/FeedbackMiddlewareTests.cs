using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualAssistantSample.Feedback;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class FeedbackMiddlewareTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_FeedbackActivity()
        {
            // Create EventData
            var data = new EventData { Message = "Test", UserId = "Test" };
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Create EventActivity
            var eventActivity = new Activity()
            {
                ChannelId = "TestProactive",
                Type = ActivityTypes.Event,
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: $"Notification.TestProactive", name: $"Notification.Proactive"),
                Recipient = new ChannelAccount(id: $"Notification.TestProactive", name: $"Notification.Proactive"),
                Name = "BroadcastEvent",
                Value = JsonConvert.SerializeObject(data)

            };

            // Create turnContext with EventActivity
            var turnContext = new TurnContext(adapter, eventActivity);

            var testFeedback = FeedbackUtil.CreateFeedbackActivity(turnContext);
            Assert.AreEqual("Was this helpful ? (1) 👍, (2) 👎, or(3) Dismiss", testFeedback.Text);
        }

        [TestMethod]
        public async Task Test_FeedbackCommentPrompt()
        {
            // Create EventData
            var data = new EventData { Message = "Test", UserId = "Test" };
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Create EventActivity
            var eventActivity = new Activity()
            {
                ChannelId = "TestProactive",
                Type = ActivityTypes.Event,
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: $"Notification.TestProactive", name: $"Notification.Proactive"),
                Recipient = new ChannelAccount(id: $"Notification.TestProactive", name: $"Notification.Proactive"),
                Name = "BroadcastEvent",
                Value = JsonConvert.SerializeObject(data)

            };

            // Create turnContext with EventActivity
            var turnContext = new TurnContext(adapter, eventActivity);

            var testFeedback = FeedbackUtil.GetFeedbackCommentPrompt(turnContext);
            Assert.AreEqual("Thanks, I appreciate your feedback. Please add any additional comments in the chat. (1) Dismiss", testFeedback.Text);
        }
    }
}
