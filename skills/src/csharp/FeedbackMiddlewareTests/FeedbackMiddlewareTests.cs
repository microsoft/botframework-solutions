using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Utilities.FeedbackMiddleware;

namespace FeedbackMiddlewareTest
{
    [TestClass]
    public class FeedbackMiddlewareTests
    {
        private static readonly FeedbackOptions DefaultFeedbackOptions = new FeedbackOptions()
        {
            FeedbackResponse = "Thanks for your feedback!",
            FeedbackActions = new List<FeedbackAction>()
            {
                new FeedbackAction()
                {
                    Text = "👍 good answer"
                },
                new FeedbackAction()
                {
                    Text = "👎 bad answer"
                }
            },
            DismissAction = new FeedbackAction()
            {
                Text = "dismiss"
            },
            FreeFormPrompt = "Please add any additional comments in the chat",
            PromptFreeForm = new PromptFreeForm()
            {
                UserCanGiveFreeForm = false
            }
        };

        private static readonly FeedbackOptions CustomizedFeedbackOptions = new FeedbackOptions()
        {
            FeedbackResponse = "Thanks for your feedback!",
            FeedbackActions = new List<FeedbackAction>()
            {
                new FeedbackAction()
                {
                    Text = "👍 good answer"
                },
                new FeedbackAction()
                {
                    Text = "👎 bad answer"
                }
            },
            DismissAction = new FeedbackAction()
            {
                Text = "dismiss"
            },
            FreeFormPrompt = "Please add any additional comments in the chat",
            PromptFreeForm = new PromptFreeForm()
            {
                PromptFreeFormAction = new List<FeedbackAction>()
                {
                    new FeedbackAction()
                    {
                        Text = "👎 bad answer"
                    }
                }
            }
        };

        private ConversationState ConvState { get; set; } = new ConversationState(new MemoryStorage());

        [TestMethod]
        [TestCategory("Middleware")]
        public async Task NonFeedback()
        {
            TestAdapter adapter = new TestAdapter().Use(new FeedbackMiddleware(ConvState));
            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                await context.SendActivityAsync("bot response");
            })
            .Send("hello world")
            .AssertReply("bot response")
            .StartTestAsync();
        }

        [TestMethod]
        [TestCategory("Middleware")]
        public async Task SendAskFeedbackActivity()
        {
            TestAdapter adapter = new TestAdapter().Use(new FeedbackMiddleware(ConvState));

            var expectedSuggestedActionsNum = DefaultFeedbackOptions.FeedbackActions.Count;
            if (DefaultFeedbackOptions.DismissAction != null)
            {
                expectedSuggestedActionsNum++;
            }

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                var feedbackActivity = await FeedbackMiddleware.CreateFeedbackMessage(context, "the answer is 123");
                Assert.AreEqual(feedbackActivity.SuggestedActions.Actions.Count, expectedSuggestedActionsNum);
                var state = ConvState.Get(context);
                Assert.IsNotNull(state["Feedback"]);
                await FeedbackMiddleware.SendFeedbackActivity(context, "the answer is 123");
            })
            .Send("what is 100 + 20 + 3?")
            .AssertReply((activity) =>
            {
                var reply = activity as Activity;
                Assert.AreEqual(reply.Text, "the answer is 123");
                Assert.AreEqual(reply.SuggestedActions.Actions.Count, expectedSuggestedActionsNum);
            })
            .StartTestAsync();
        }

        [TestMethod]
        [TestCategory("Middleware")]
        public async Task SendGetFeedbackActivity()
        {
            TestAdapter adapter = new TestAdapter(sendTraceActivity: true)
                .Use(new MockPendingFeedbackMiddleware(ConvState, MockPendingFeedbackMiddleware.MockFeedbackRecordWithoutComments))
                .Use(new FeedbackMiddleware(ConvState));

            await new TestFlow(adapter)
            .Send(DefaultFeedbackOptions.FeedbackActions[0].Text)
            .AssertReply((activity) =>
            {
                var reply = activity as Activity;
                Assert.AreEqual(reply.Type, ActivityTypes.Trace);
            })
            .AssertReply((activity) =>
            {
                var reply = activity as Activity;
                Assert.AreEqual(reply.Text, DefaultFeedbackOptions.FeedbackResponse);
            })
            .StartTestAsync();
        }

        [TestMethod]
        [TestCategory("Middleware")]
        public async Task SendAskCommentsActivity()
        {
            TestAdapter adapter = new TestAdapter()
                .Use(new MockPendingFeedbackMiddleware(ConvState, MockPendingFeedbackMiddleware.MockFeedbackRecordWithoutComments))
                .Use(new FeedbackMiddleware(ConvState, CustomizedFeedbackOptions));

            await new TestFlow(adapter)
            .Send(CustomizedFeedbackOptions.FeedbackActions[1].Text)
            .AssertReply((activity) =>
            {
                var reply = activity as Activity;
                Assert.AreEqual(reply.Text, CustomizedFeedbackOptions.FreeFormPrompt);
            })
            .StartTestAsync();
        }

        [TestMethod]
        [TestCategory("Middleware")]
        public async Task SendGetCommentsActivity()
        {
            TestAdapter adapter = new TestAdapter(sendTraceActivity: true)
                .Use(new MockPendingFeedbackMiddleware(ConvState, MockPendingFeedbackMiddleware.MockFeedbackRecordWithComments))
                .Use(new FeedbackMiddleware(ConvState));

            await new TestFlow(adapter)
            .Send("my comments")
            .AssertReply((activity) =>
            {
                var reply = activity as Activity;
                Assert.AreEqual(reply.Type, ActivityTypes.Trace);
            })
            .AssertReply((activity) =>
            {
                var reply = activity as Activity;
                Assert.AreEqual(reply.Text, DefaultFeedbackOptions.FeedbackResponse);
            })
            .StartTestAsync();
        }
    }
}
