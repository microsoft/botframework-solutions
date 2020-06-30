using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Feedback;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Bot.Solutions.Tests.Middleware
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class TeamsReactionMiddlewareTests
    {
        [TestMethod]
        public async Task TeamsMiddlewareTest()
        {
            var botTelemetryClient = new Mock<IBotTelemetryClient>();
            var storage = new MemoryStorage();

            var conversation = TestAdapter.CreateConversation("Name");
            conversation.User.Role = "user";

            var adapter = new TestAdapter(conversation)
                .Use(new TeamsReactionMiddleware(botTelemetryClient.Object));

            var teamsReactionActivity = new Activity
            {
                ChannelId = "msteams",
                Type = ActivityTypes.MessageReaction,
                ReactionsAdded = new List<MessageReaction> { new MessageReaction { Type = "Liked" } },
            };

            var response = "Response";
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Type == ActivityTypes.MessageReaction)
                {
                    Assert.IsNotNull(context.Activity);
                }
                else
                {
                    await context.SendActivityAsync(context.Activity.CreateReply(response));
                }
            })
            .Send(teamsReactionActivity)
            .StartTestAsync();
        }
    }
}
