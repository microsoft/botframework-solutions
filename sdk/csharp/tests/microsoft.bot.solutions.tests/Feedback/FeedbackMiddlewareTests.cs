// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
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
        private readonly string neutralFeedback = "neutral";

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
                .AssertReply("Thanks, I appreciate your feedback.")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task DefaultOptions_Negative()
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
                .Send(negativeFeedback)
                .AssertReply((activity) =>
                {
                    Assert.AreEqual(activity.AsMessageActivity().Text, "Thanks, I appreciate your feedback. Please add any additional comments in the chat.");
                    var card = activity.AsMessageActivity().Attachments[0].Content as HeroCard;
                    Assert.AreEqual(card.Buttons.Count, 1);
                })
                .Send("comment")
                .AssertReply("Your comment has been received.")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task DefaultOptions_CommentDirectly()
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
                .Send("comment")
                .AssertReply("Your comment has been received.")
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CustomOptions_Neutral()
        {
            var storage = new MemoryStorage();
            var convState = new ConversationState(storage);

            var adapter = new TestAdapter(TestAdapter.CreateConversation("Name"))
                .Use(new FeedbackMiddleware(convState, new NullBotTelemetryClient(), CreateCustomOptions()));

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
                    Assert.AreEqual(card.Buttons.Count, 4);
                })
                .Send(neutralFeedback)
                .AssertReply((activity) =>
                {
                    Assert.AreEqual(activity.AsMessageActivity().Text, "Please add any additional comments in the chat.");
                    var card = activity.AsMessageActivity().Attachments[0].Content as HeroCard;
                    Assert.AreEqual(card.Buttons.Count, 1);
                })
                .Send("comment")
                .AssertReply("comment")
                .StartTestAsync();
        }

        private FeedbackOptions CreateCustomOptions()
        {
            var options = new FeedbackOptions
            {
                FeedbackActions = (ITurnContext context, string tag) =>
                {
                    return new List<CardAction>()
                    {
                        new CardAction(ActionTypes.PostBack, title: "👍", value: positiveFeedback),
                        new CardAction(ActionTypes.PostBack, title: "😑", value: neutralFeedback),
                        new CardAction(ActionTypes.PostBack, title: "👎", value: negativeFeedback),
                    };
                },
                FeedbackReceivedMessage = (ITurnContext context, string tag, CardAction action) =>
                {
                    if ((string)action.Value == neutralFeedback)
                    {
                        return (context.Activity.CreateReply(FeedbackResponses.CommentPrompt), true);
                    }
                    else
                    {
                        return (context.Activity.CreateReply(FeedbackResponses.FeedbackReceivedMessage), false);
                    }
                },
                CommentReceivedMessage = (ITurnContext context, string tag, CardAction action, string comment) =>
                {
                    return context.Activity.CreateReply(comment);
                },
            };
            return options;
        }
    }
}
