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
    [TestCategory("Greeting")]
    public class GreetingTests : BotTestBase
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

        /// <summary>
        /// Assert that a new user is greeted and begins onboarding prompt.
        /// </summary>
        /// <param name="cancellationTokenSource">CancellationTokenSource.</param>
        /// <param name="testBot">TestBotClient.</param>
        /// <param name="useComplexInputWithName">Boolean flag to determine user response to onboarding prompt.</param>
        /// <returns>Task.</returns>
        public async Task Assert_New_User_Greeting(CancellationTokenSource cancellationTokenSource, TestBotClient testBot, bool useComplexInputWithName = false)
        {
            var profileState = new UserProfileState { Name = GeneralUtterances.Name };
            var newUserIntroCardTitleVariations = AllResponsesTemplates.ExpandTemplate("NewUserIntroCardTitle", profileState);

            var namePromptVariations = AllResponsesTemplates.ExpandTemplate("NamePrompt");
            var allHaveMessageVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessage", profileState);

            await testBot.SendEventAsync("startConversation", cancellationTokenSource.Token);

            // Wait 5 seconds before checking for response on a new conversation
            await Task.Delay(TimeSpan.FromSeconds(5));

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            testBot.VerifyAdaptiveCard(newUserIntroCardTitleVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));

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

        /// <summary>
        /// Assert that a returning user is greeted.
        /// </summary>
        /// <param name="cancellationTokenSource">CancellationTokenSource.</param>
        /// <param name="testBot">TestBotClient.</param>
        /// <returns>Task.</returns>
        private async Task Assert_Returning_User_Greeting(CancellationTokenSource cancellationTokenSource, TestBotClient testBot)
        {
            var profileState = new UserProfileState { Name = GeneralUtterances.Name };
            var returningUserIntroCardTitleVariations = AllResponsesTemplates.ExpandTemplate("ReturningUserIntroCardTitle", profileState);

            await testBot.SendEventAsync("startConversation", cancellationTokenSource.Token);

            // Wait 3000 milliseconds before checking for response on a new conversation
            await Task.Delay(3000);

            var messages = await testBot.ReadBotMessagesAsync(cancellationTokenSource.Token);

            var activities = messages.ToList();

            testBot.VerifyAdaptiveCard(returningUserIntroCardTitleVariations, activities.FirstOrDefault(m => m.Attachments != null && m.Attachments.Any()));
        }
    }
}