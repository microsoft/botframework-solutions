using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Tests.Middleware
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class EventDebuggerMiddlewareTests
    {
        [TestMethod]
        public async Task DefaultOptions_EventText()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var conversation = TestAdapter.CreateConversation("Name");
            conversation.ChannelId = Connector.Channels.Test;

            // Create EventActivity
            var eventActivity = new Activity()
            {
                ChannelId = "TestProactive",
                Type = ActivityTypes.Message,
                Name = "BroadcastEvent",
                Value = "Test",
            };

            // Create MessageActivity
            var messageActivity = new Activity()
            {
                ChannelId = "TestProactive",
                Type = ActivityTypes.Message,
                Name = "BroadcastEvent",
                Text = "/event:" + JsonConvert.SerializeObject(eventActivity),
                Value = "Test",
            };

            var adapter = new TestAdapter(conversation)
                .Use(new EventDebuggerMiddleware());

            var response = "Response";
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Type == ActivityTypes.Event)
                {
                    var activity = context.Activity;
                    Assert.IsNotNull(activity);
                }
                else
                {
                    await context.SendActivityAsync(context.Activity.CreateReply(response));
                }
            })
            .Send(messageActivity).StartTestAsync();
        }

        [TestMethod]
        public async Task DefaultOptions_EventValue()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var conversation = TestAdapter.CreateConversation("Name");
            conversation.ChannelId = Connector.Channels.Test;

            // Create MessageActivity
            var messageActivity = new Activity()
            {
                ChannelId = "Event",
                Type = ActivityTypes.Message,
                Value = @"{
                  'event':
                    {
                      'name': 'BroadcastEvent',
                      'text': 'test',
                      'value': 'test'
                    }
                }",
            };

            var adapter = new TestAdapter(conversation)
                .Use(new EventDebuggerMiddleware());

            var response = "Response";
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Type == ActivityTypes.Event)
                {
                    var activity = context.Activity;
                    Assert.IsNotNull(activity);
                }
                else
                {
                    await context.SendActivityAsync(context.Activity.CreateReply(response));
                }
            })
            .Send(messageActivity).StartTestAsync();
        }
    }
}
