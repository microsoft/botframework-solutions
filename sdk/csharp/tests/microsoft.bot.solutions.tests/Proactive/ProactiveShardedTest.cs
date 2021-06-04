using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Proactive.Sharded;
using Microsoft.Bot.Solutions.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Proactive
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ProactiveShardedTest
    {
        [TestMethod]
        public async Task DefaultOptions()
        {
            var microsoftAppId = string.Empty;

            var storage = new MemoryStorage();

            var state = new ProactiveStateSharded(storage);
            var proactiveStateAccessor = state.CreateProperty<ProactiveModel>(nameof(ProactiveModel));

            var conversation = TestAdapter.CreateConversation(ProactiveTestConstants.Name);
            conversation.User.Role = ProactiveTestConstants.User;

            var adapter = new TestAdapter(conversation)
                .Use(new ProactiveStateMiddlewareSharded(state));

            var response = ProactiveTestConstants.Response;
            var proactiveResponse = ProactiveTestConstants.ProactiveResponse;
            var proactiveEvent = new Activity(type: ActivityTypes.Event, value: ProactiveTestConstants.User1, text: proactiveResponse);

            await new TestFlow(adapter, async (context, cancellationToken) =>
            {
                if (context.Activity.Type == ActivityTypes.Event)
                {
                    var proactiveModel = await proactiveStateAccessor.GetAsync(context, () => new ProactiveModel());

                    var hashedUserId = MD5Util.ComputeHash(context.Activity.Value.ToString());

                    var conversationReference = proactiveModel[hashedUserId].Conversation;

                    await context.Adapter.ContinueConversationAsync(microsoftAppId, conversationReference, ContinueConversationCallback(context, context.Activity.Text), cancellationToken);
                }
                else
                {
                    await context.SendActivityAsync(context.Activity.CreateReply(response));
                }
            })
                .Send("foo")
                .AssertReply(response)
                .Send(proactiveEvent)
                .AssertReply(proactiveResponse)
                .StartTestAsync();
        }

        private BotCallbackHandler ContinueConversationCallback(ITurnContext context, string message)
        {
            return async (turnContext, cancellationToken) =>
            {
                var activity = turnContext.Activity.CreateReply(message);
                await turnContext.SendActivityAsync(activity);
            };
        }
    }
}
