// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.FunctionalTests.Configuration;
using VirtualAssistantSample.Tests;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    [TestCategory("QnAMaker")]
    public class QnAMakerTests : BotTestBase
    {
        private CancellationTokenSource cancellationTokenSource;
        private TestBotClient testBot;

        [TestInitialize]
        public void Setup()
        {
            cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            testBot = new TestBotClient(new EnvironmentBotTestConfiguration());
        }

        [TestMethod]
        public async Task Test_QnAMaker_ChitChat()
        {
            await Assert_QnA_ChitChat_Responses();
        }

        [TestMethod]
        public async Task Test_QnAMaker_FAQ()
        {
            await Assert_QnA_FAQ_Responses();
        }

        /// <summary>
        /// Assert that a Qna Maker (ChitChat) is working.
        /// </summary>
        private async Task Assert_QnA_ChitChat_Responses()
        {
            await testBot.StartConversationAsync(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.ChitChat, cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("I don't have a name.", cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that a Qna Maker (FAQ) is working.
        /// </summary>
        private async Task Assert_QnA_FAQ_Responses()
        {
            await testBot.StartConversationAsync(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.FAQ, cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Raise an issue on the [GitHub repo](https://aka.ms/virtualassistant)", cancellationTokenSource.Token);
        }
    }
}