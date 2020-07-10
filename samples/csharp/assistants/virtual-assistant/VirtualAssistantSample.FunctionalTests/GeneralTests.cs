// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.FunctionalTests.Configuration;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Tests;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    [TestCategory("General")]
    public class GeneralTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_General_Cancel()
        {
            await Assert_General_Cancel();
        }

        [TestMethod]
        public async Task Test_General_Escalate()
        {
            await Assert_General_Escalate();
        }

        [TestMethod]
        public async Task Test_General_Help()
        {
            await Assert_General_Help();
        }

        [TestMethod]
        public async Task Test_General_Logout()
        {
            await Assert_General_Logout();
        }

        [TestMethod]
        public async Task Test_General_Repeat()
        {
            await Assert_General_Repeat();
        }

        [TestMethod]
        public async Task Test_General_StartOver()
        {
            await Assert_General_StartOver();
        }

        /// <summary>
        /// Assert that Cancel intent is working.
        /// </summary>
        private async Task Assert_General_Cancel()
        {
            var cancelledMessageVariations = AllResponsesTemplates.ExpandTemplate("CancelledMessage");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Cancel, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(cancelledMessageVariations, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that Escalate intent is working.
        /// </summary>
        private async Task Assert_General_Escalate()
        {
            var escalateMessageVariations = AllResponsesTemplates.ExpandTemplate("EscalatedText");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Escalate, cancellationTokenSource.Token);

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            testBot.VerifyHeroCard(escalateMessageVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));
        }

        /// <summary>
        /// Assert that Help intent is working.
        /// </summary>
        private async Task Assert_General_Help()
        {
            var helpMessageVariations = AllResponsesTemplates.ExpandTemplate("HelpText");
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Help, cancellationTokenSource.Token);

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            testBot.VerifyHeroCard(helpMessageVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));
        }

        /// <summary>
        /// Assert that Logout intent is working with an unauthenticated user.
        /// </summary>
        private async Task Assert_General_Logout()
        {
            var profileState = new UserProfileState { Name = GeneralUtterances.Name };
            var logoutMessageVariations = AllResponsesTemplates.ExpandTemplate("LogoutMessage", profileState);

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Logout, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(logoutMessageVariations, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that Repeat intent is working.
        /// </summary>
        private async Task Assert_General_Repeat()
        {
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Repeat, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(firstPromptVariations, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that StartOver intent is working.
        /// </summary>
        private async Task Assert_General_StartOver()
        {
            var startOverMessageVariations = AllResponsesTemplates.ExpandTemplate("StartOverMessage");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await new GreetingTests().Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.StartOver, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(startOverMessageVariations, cancellationTokenSource.Token);
        }
    }
}