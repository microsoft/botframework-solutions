// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using VirtualAssistantSample.Models;
using VirtualAssistantSample.Tests;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    [TestCategory("Core")]
    public class CoreTests : DirectLineClientTestBase
    {
        [TestMethod]
        public async Task Test_Greeting_NewAndReturningUser()
        {
            await Assert_New_User_Greeting();
            await Assert_Returning_User_Greeting();
        }

        [TestMethod]
        public async Task Test_Onboarding_ExtractPersonName()
        {
            await Assert_New_User_Greeting(useComplexInputWithName: true);
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
        public async Task<Conversation> Assert_New_User_Greeting(bool useComplexInputWithName = false)
        {
            var profileState = new UserProfileState { Name = TestName };
            var allNamePromptVariations = AllResponsesTemplates.ExpandTemplate("NamePrompt");
            var allHaveMessageVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessage", profileState);
            var newUserIntroCardTitleVariations = AllResponsesTemplates.ExpandTemplate("NewUserIntroCardTitle");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());
            Assert.AreEqual("application/vnd.microsoft.card.adaptive", responses[0]?.Attachments[0]?.ContentType);
            var cardContent = JsonConvert.DeserializeObject<AdaptiveCard>(responses[0].Attachments[0].Content.ToString());
            CollectionAssert.Contains(newUserIntroCardTitleVariations as ICollection, cardContent.Speak);
            CollectionAssert.Contains(allNamePromptVariations as ICollection, responses[1].Text);

            // Send user input of either name or "My name is X"
            if (useComplexInputWithName)
            {
                var complexInputWithName = $"My name is {TestName}";
                responses = await SendActivityAsync(conversation, CreateMessageActivity(complexInputWithName));

                CollectionAssert.Contains(allHaveMessageVariations as ICollection, responses[2].Text);
            }
            else
            {
                responses = await SendActivityAsync(conversation, CreateMessageActivity(TestName));

                CollectionAssert.Contains(allHaveMessageVariations as ICollection, responses[2].Text);
            }

            return conversation;
        }

        /// <summary>
        /// Assert that a returning user is only greeted with a single card activity and the welcome back prompt.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Task.</returns>
        public async Task Assert_Returning_User_Greeting()
        {
            var profileState = new UserProfileState { Name = TestName };
            var returningUserIntroCardTitleVariations = AllResponsesTemplates.ExpandTemplate("ReturningUserIntroCardTitle", profileState);

            var conversation = await StartBotConversationAsync();

            // Returning user card and welcome message represent the first two messages
            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // First Activity should have an adaptive card response.
            Assert.AreEqual("application/vnd.microsoft.card.adaptive", responses[0]?.Attachments[0]?.ContentType);
            var cardContent = JsonConvert.DeserializeObject<AdaptiveCard>(responses[0].Attachments[0].Content.ToString());
            CollectionAssert.Contains(returningUserIntroCardTitleVariations as ICollection, cardContent.Speak);
        }

        /// <summary>
        /// Assert that a Qna Maker (ChitChat) is working.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Task.</returns>
        public async Task Assert_QnA_ChitChat_Responses()
        {
            var conversation = await Assert_New_User_Greeting();

            // Chit-chat response
            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.ChitChat));
            Assert.AreEqual("I don't have a name.", responses[4].Text);
        }

        /// <summary>
        /// Assert that a Qna Maker (FAQ) is working.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Task.</returns>
        public async Task Assert_QnA_FAQ_Responses()
        {
            var conversation = await Assert_New_User_Greeting();

            // FAQ response
            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.FAQ));
            Assert.AreEqual("Raise an issue on the [GitHub repo](https://aka.ms/virtualassistant)", responses[4].Text);
        }

        /// <summary>
        /// Assert that Cancel intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Cancel()
        {
            var cancelledMessageVariations = AllResponsesTemplates.ExpandTemplate("CancelledMessage");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await Assert_New_User_Greeting();

            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Cancel));
            CollectionAssert.Contains(cancelledMessageVariations as ICollection, responses[4].Text);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[5].Text);
        }

        /// <summary>
        /// Assert that Escalate intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Escalate()
        {
            var escalateMessageVariations = AllResponsesTemplates.ExpandTemplate("EscalatedText");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await Assert_New_User_Greeting();

            // Assert that escalate hero card is returned
            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Escalate));
            Assert.AreEqual(1, responses[4].Attachments.Count);
            Assert.AreEqual("application/vnd.microsoft.card.hero", responses[4].Attachments[0].ContentType);

            var cardContent = JsonConvert.DeserializeObject<HeroCard>(responses[4].Attachments[0].Content.ToString());
            CollectionAssert.Contains(escalateMessageVariations as ICollection, cardContent.Subtitle);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[5].Text);
        }

        /// <summary>
        /// Assert that Help intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Help()
        {
            var helpMessageVariations = AllResponsesTemplates.ExpandTemplate("HelpText");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await Assert_New_User_Greeting();

            // Assert that help hero card is returned
            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Help));
            Assert.AreEqual(1, responses[4].Attachments.Count);
            Assert.AreEqual("application/vnd.microsoft.card.hero", responses[4].Attachments[0].ContentType);
            var cardContent = JsonConvert.DeserializeObject<HeroCard>(responses[4].Attachments[0].Content.ToString());
            CollectionAssert.Contains(helpMessageVariations as ICollection, cardContent.Subtitle);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[5].Text);
        }

        /// <summary>
        /// Assert that Logout intent is working with an unauthenticated user.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Logout()
        {
            var profileState = new UserProfileState { Name = TestName };
            var logoutMessageVariations = AllResponsesTemplates.ExpandTemplate("LogoutMessage", profileState);
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await Assert_New_User_Greeting();

            // Assert that logout response is returned
            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Logout));
            CollectionAssert.Contains(logoutMessageVariations as ICollection, responses[4].Text);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[5].Text);
        }

        /// <summary>
        /// Assert that Repeat intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Repeat()
        {
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await Assert_New_User_Greeting();

            // Assert that repeat response is returned
            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Repeat));
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[5].Text);
        }

        /// <summary>
        /// Assert that StartOver intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_StartOver()
        {
            var startOverMessageVariations = AllResponsesTemplates.ExpandTemplate("StartOverMessage");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await Assert_New_User_Greeting();

            // Assert that start over response is returned
            var responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.StartOver));
            CollectionAssert.Contains(startOverMessageVariations as ICollection, responses[4].Text);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[5].Text);
        }
    }
}