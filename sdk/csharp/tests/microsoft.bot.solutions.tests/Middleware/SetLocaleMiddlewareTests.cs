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
    public class SetLocaleMiddlewareTests
    {
        [TestMethod]
        public async Task DefaultOptions()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var conversation = TestAdapter.CreateConversation("Name");
            conversation.ChannelId = Connector.Channels.Test;

            // Create MessageActivity
            var messageActivity = new Activity()
            {
                ChannelId = "TestProactive",
                Type = ActivityTypes.Message,
                Name = "BroadcastEvent",
                Text = "Test",
                Value = "Test",
            };

            var adapter = new TestAdapter(conversation)
                .Use(new SetLocaleMiddleware("en-us"));

            var response = "Response";
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Type == ActivityTypes.Message)
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
