// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Feedback;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Feedback
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class FeedbackMiddlewareTests
    {
        private readonly string positiveFeedback = "positive";
        private readonly string negativeFeedback = "negative";

        [TestMethod]
        public async Task DefaultOptions_Positive()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var adapter = new TestAdapter(TestAdapter.CreateConversation("Name"))
                .Use(new FeedbackMiddleware(convState, new NullBotTelemetryClient()));

            var response = "Response";
            var tag = "Tag";

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                await context.SendActivityAsync(response);
                await FeedbackMiddleware.RequestFeedbackAsync(context, tag);

                // TODO save manualy
                await convState.SaveChangesAsync(context, false, cancellationToken);
            })
                .Send("foo")
                .AssertReply(response)
                .AssertReply((activity) =>
                {
                    var card = activity.AsMessageActivity().Attachments[0].Content as HeroCard;
                    Assert.AreEqual(card.Buttons.Count, 3);
                })
                .Send(positiveFeedback)
                .AssertReply("Thanks for your feedback!")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task DefaultOptions_Comment()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var adapter = new TestAdapter(TestAdapter.CreateConversation("Name"))
                .Use(new FeedbackMiddleware(convState, new NullBotTelemetryClient(), new FeedbackOptions
                {
                    CommentsEnabled = true,
                }));

            var response = "Response";
            var tag = "Tag";

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                await context.SendActivityAsync(response);
                await FeedbackMiddleware.RequestFeedbackAsync(context, tag);

                // TODO save manualy
                await convState.SaveChangesAsync(context, false, cancellationToken);
            })
                .Send("foo")
                .AssertReply(response)
                .AssertReply((activity) =>
                {
                    var card = activity.AsMessageActivity().Attachments[0].Content as HeroCard;
                    Assert.AreEqual(card.Buttons.Count, 3);
                })
                .Send(negativeFeedback)
                .AssertReply((activity) =>
                {
                    var card = activity.AsMessageActivity().Attachments[0].Content as HeroCard;
                    Assert.AreEqual(card.Text, "Please add any additional comments in the chat.");
                    Assert.AreEqual(card.Buttons.Count, 1);
                })
                .Send("comment")
                .AssertReply("Your comment has been received.")
                .StartTestAsync();
        }
    }
}
