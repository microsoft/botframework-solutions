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
    public class FeedbackTests : BotTestBase
    {
        [TestMethod]
        public void Test_FeedbackActivity()
        {
            // Get TestAdapter
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Create EventActivity
            var eventActivity = new Activity()
            {
                ChannelId = "Test",
                Type = ActivityTypes.Message,
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: "Test", name: "Test"),
                Recipient = new ChannelAccount(id: "Test", name: "Test"),
                Value = "Test"
            };

            // Create turnContext with EventActivity
            var turnContext = new TurnContext(adapter, eventActivity);

            var testFeedback = FeedbackUtil.CreateFeedbackActivity(turnContext);
            Assert.IsTrue(testFeedback.Text != null && testFeedback.Text.Contains(FeedbackResponses.FeedbackPromptMessage));
        }

        [TestMethod]
        public void Test_FeedbackCommentPrompt()
        {
            // Get TestAdapter
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Create EventActivity
            var eventActivity = new Activity()
            {
                ChannelId = "Test",
                Type = ActivityTypes.Message,
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: "Test", name: "Test"),
                Recipient = new ChannelAccount(id: "Test", name: "Test"),
                Value = "Test"
            };

            // Create turnContext with EventActivity
            var turnContext = new TurnContext(adapter, eventActivity);

            var testFeedback = FeedbackUtil.GetFeedbackCommentPrompt(turnContext);
            Assert.IsTrue(testFeedback.Text != null && testFeedback.Text.Contains(FeedbackResponses.FeedbackReceivedMessage));
        }
    }
}
