using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using VirtualAssistantSample.Models;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ProactiveTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_ProactiveEvent()
        {
            // Create EventData
            var data = new EventData { Message = "Test", UserId = "Test" };
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            var proactiveState = sp.GetService<ProactiveState>();

            // Create ProactiveStateAccessor
            var proactiveStateAccessor = proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));

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

            // Create Proactive Subscription
            var proactiveSub = await proactiveStateAccessor.GetAsync(
            turnContext,
            () => new ProactiveModel(),
            CancellationToken.None)
            .ConfigureAwait(false);

            // Store Activity and Thread Id
            proactiveSub[MD5Util.ComputeHash("Test")] = new ProactiveModel.ProactiveData
            {
                Conversation = eventActivity.GetConversationReference(),
            };

            // Save changes to proactive model
            await proactiveStateAccessor.SetAsync(turnContext, proactiveSub).ConfigureAwait(false);
            await proactiveState.SaveChangesAsync(turnContext).ConfigureAwait(false);

            // Validate EventActivity Response
            await GetTestFlow()
                .Send(eventActivity)
                .AssertReply(activity => Assert.AreEqual("Test", activity.AsMessageActivity().Text))
                .StartTestAsync();
        }
    }
}
