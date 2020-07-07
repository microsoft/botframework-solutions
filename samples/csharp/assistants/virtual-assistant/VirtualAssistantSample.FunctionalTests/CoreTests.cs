// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using VirtualAssistantSample.FunctionalTests.Configuration;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Tests;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    [TestCategory("Core")]
    public class CoreTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Greeting_NewAndReturningUser()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);

            await Assert_New_User_Greeting(cancellationTokenSource, testBot);

            // Create a new TestBotClient with same user as previous conversation
            var user = testBot.GetUser();
            testBot = new TestBotClient(new EnvironmentBotTestConfiguration(), user);

            await testBot.StartConversation(cancellationTokenSource.Token);

            await Assert_Returning_User_Greeting(cancellationTokenSource, testBot);
        }

        [TestMethod]
        public async Task Test_Onboarding_ExtractPersonName()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);

            await Assert_New_User_Greeting(cancellationTokenSource, testBot, useComplexInputWithName: true);
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
        /// Assert that a new user is greeted with the onboarding prompt.
        /// </summary>
        /// <param name="useComplexInputWithName">Send user name only or included in "My name is X" message.</param>
        /// <returns>Task.</returns>
        private async Task Assert_New_User_Greeting(CancellationTokenSource cancellationTokenSource, TestBotClient testBot, bool useComplexInputWithName = false)
        {
            var profileState = new UserProfileState { Name = GeneralUtterances.Name };
            var newUserIntroCardTitleVariations = AllResponsesTemplates.ExpandTemplate("NewUserIntroCardTitle", profileState);

            var namePromptVariations = AllResponsesTemplates.ExpandTemplate("NamePrompt");
            var allHaveMessageVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessage", profileState);

            await testBot.SendEventAsync("startConversation", cancellationTokenSource.Token);

            // Wait 3000 milliseconds before checking for response on a new conversation
            await Task.Delay(3000);

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            await testBot.VerifyAdaptiveCard(newUserIntroCardTitleVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));

            // Send user input of either name or "My name is X"
            if (useComplexInputWithName)
            {
                await testBot.SendMessageAsync($"My name is {GeneralUtterances.Name}", cancellationTokenSource.Token);
                await testBot.AssertReplyOneOf(allHaveMessageVariations, cancellationTokenSource.Token);
            }
            else
            {
                await testBot.SendMessageAsync(GeneralUtterances.Name, cancellationTokenSource.Token);
                await testBot.AssertReplyOneOf(allHaveMessageVariations, cancellationTokenSource.Token);
            }
        }

        public async Task Assert_Returning_User_Greeting(CancellationTokenSource cancellationTokenSource, TestBotClient testBot)
        {
            var profileState = new UserProfileState { Name = GeneralUtterances.Name };
            var returningUserIntroCardTitleVariations = AllResponsesTemplates.ExpandTemplate("ReturningUserIntroCardTitle", profileState);

            await testBot.SendEventAsync("startConversation", cancellationTokenSource.Token);
            // Wait 3000 milliseconds before checking for response on a new conversation
            await Task.Delay(3000);

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            await testBot.VerifyAdaptiveCard(returningUserIntroCardTitleVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));
        }

        /// <summary>
        /// Assert that a Qna Maker (ChitChat) is working.
        /// </summary>
        public async Task Assert_QnA_ChitChat_Responses()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.ChitChat, cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("I don't have a name.", cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that a Qna Maker (FAQ) is working.
        /// </summary>
        public async Task Assert_QnA_FAQ_Responses()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.FAQ, cancellationTokenSource.Token);
            await testBot.AssertReplyAsync("Raise an issue on the [GitHub repo](https://aka.ms/virtualassistant)", cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that Cancel intent is working.
        /// </summary>
        public async Task Assert_General_Cancel()
        {
            var cancelledMessageVariations = AllResponsesTemplates.ExpandTemplate("CancelledMessage");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Cancel, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(cancelledMessageVariations, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that Escalate intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Escalate()
        {
            var escalateMessageVariations = AllResponsesTemplates.ExpandTemplate("EscalatedText");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Escalate, cancellationTokenSource.Token);

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            await testBot.VerifyHeroCard(escalateMessageVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));
        }

        /// <summary>
        /// Assert that Help intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Help()
        {
            var helpMessageVariations = AllResponsesTemplates.ExpandTemplate("HelpText");
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Help, cancellationTokenSource.Token);

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            await testBot.VerifyHeroCard(helpMessageVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));
        }

        /// <summary>
        /// Assert that Logout intent is working with an unauthenticated user.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Logout()
        {
            var profileState = new UserProfileState { Name = GeneralUtterances.Name };
            var logoutMessageVariations = AllResponsesTemplates.ExpandTemplate("LogoutMessage", profileState);

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Logout, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(logoutMessageVariations, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that Repeat intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Repeat()
        {
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.Repeat, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(firstPromptVariations, cancellationTokenSource.Token);
        }

        /// <summary>
        /// Assert that StartOver intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_StartOver()
        {
            var startOverMessageVariations = AllResponsesTemplates.ExpandTemplate("StartOverMessage");

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversation(cancellationTokenSource.Token);
            await Assert_New_User_Greeting(cancellationTokenSource, testBot);
            await testBot.SendMessageAsync(GeneralUtterances.StartOver, cancellationTokenSource.Token);
            await testBot.AssertReplyOneOf(startOverMessageVariations, cancellationTokenSource.Token);
        }
    }
}