// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.Extensions
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class DialogContextExtensionTests
    {
        [TestMethod]
        public async Task Test_DialogContextSuppressCompletionMessage()
        {
            // Create MessageActivity
            // Create mock Activity for testing.
            var tokenResponseActivity = new Activity { Type = ActivityTypes.Message, Value = new TokenResponse { Token = "test", ChannelId = Connector.Channels.Test, ConnectionName = "testevent" }, Name = "testevent", ChannelId = Connector.Channels.Test };

            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");
            var adapter = new TestAdapter()
            .Use(new AutoSaveStateMiddleware(convoState));

            var dialogs = new DialogSet(dialogState);
            BotCallbackHandler botCallbackHandler = async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);
                var suppress = dc.SuppressCompletionMessage();
                Assert.IsFalse(suppress);
            };

            await new TestFlow(adapter, botCallbackHandler)
            .Send("hello").StartTestAsync();
        }
    }
}
