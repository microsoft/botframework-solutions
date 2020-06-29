// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            await Assert_New_User_Greeting(useComplexResponseWithName: true);
        }

        [TestMethod]
        public async Task Test_QnAMaker_ChitChat()
        {
            await Assert_New_User_Greeting();
            await Assert_QnA_ChitChat_Responses();
        }

        [TestMethod]
        public async Task Test_QnAMaker_FAQ()
        {
            await Assert_New_User_Greeting();
            await Assert_QnA_FAQ_Responses();
        }

        [TestMethod]
        public async Task Test_General_Cancel()
        {
            await Assert_New_User_Greeting();
            await Assert_General_Cancel();
        }

        [TestMethod]
        public async Task Test_General_Escalate()
        {
            await Assert_New_User_Greeting();
            await Assert_General_Escalate();
        }

        [TestMethod]
        public async Task Test_General_Help()
        {
            await Assert_New_User_Greeting();
            await Assert_General_Help();

        }

        [TestMethod]
        public async Task Test_General_Logout()
        {
            await Assert_New_User_Greeting();
            await Assert_General_Logout();
        }

        [TestMethod]
        public async Task Test_General_Repeat()
        {
            await Assert_New_User_Greeting();
            await Assert_General_Repeat();
        }

        [TestMethod]
        public async Task Test_General_StartOver()
        {
            await Assert_New_User_Greeting();
            await Assert_General_StartOver();
        }

        /// <summary>
        /// Assert that a new user is greeted with the onboarding prompt. Test 
        /// </summary>
        /// <param name="useComplexResponseWithName">Send user name only or included in "My name is X" message</param>
        /// <returns>Task.</returns>
        public async Task Assert_New_User_Greeting(bool useComplexResponseWithName = true)
        {
            var profileState = new UserProfileState { Name = TestName };

            var allNamePromptVariations = AllResponsesTemplates.ExpandTemplate("NamePrompt");
            var allHaveMessageVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessage", profileState);

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            Assert.AreEqual(1, responses[0].Attachments.Count);
            CollectionAssert.Contains(allNamePromptVariations as ICollection, responses[1].Text);

            if (useComplexResponseWithName)
            {
                var myNameIsMessage = $"My name is {TestName}";
                responses = await SendActivityAsync(conversation, CreateMessageActivity(myNameIsMessage));

                CollectionAssert.Contains(allHaveMessageVariations as ICollection, responses[2].Text);
            } 
            else
            {
                responses = await SendActivityAsync(conversation, CreateMessageActivity(TestName));

                CollectionAssert.Contains(allHaveMessageVariations as ICollection, responses[2].Text);
            }
        }

        /// <summary>
        /// Assert that a returning user is only greeted with a single card activity and the welcome back prompt.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Task.</returns>
        public async Task Assert_Returning_User_Greeting()
        {
            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // 1 response for the Adaptive Card and 1 response for the welcome back prompt
            Assert.AreEqual(2, responses.Count);

            // Both should be message Activities.
            Assert.AreEqual(ActivityTypes.Message, responses[0].GetActivityType());
            Assert.AreEqual(ActivityTypes.Message, responses[1].GetActivityType());

            // First Activity should have an adaptive card response.
            Assert.AreEqual(1, responses[0].Attachments.Count);
            Assert.AreEqual("application/vnd.microsoft.card.adaptive", responses[0].Attachments[0].ContentType);
        }

        /// <summary>
        /// Assert that a Qna Maker (ChitChat) is working.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Task.</returns>
        public async Task Assert_QnA_ChitChat_Responses()
        {
            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.ChitChat));
            Assert.AreEqual(responses[2].Text, "I don't have a name.");
        }

        /// <summary>
        /// Assert that a Qna Maker (FAQ) is working.
        /// </summary>
        /// <param name="fromUser">User identifier used for the conversation and activities.</param>
        /// <returns>Task.</returns>
        public async Task Assert_QnA_FAQ_Responses()
        {
            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.FAQ));
            Assert.AreEqual(responses[2].Text, "Raise an issue on the [GitHub repo](https://aka.ms/virtualassistant)");
        }

        /// <summary>
        /// Assert that Cancel intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Cancel()
        {
            var cancelledMessageVariations = AllResponsesTemplates.ExpandTemplate("CancelledMessage");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Cancel));

            CollectionAssert.Contains(cancelledMessageVariations as ICollection, responses[2].Text);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[3].Text);
        }

        /// <summary>
        /// Assert that Escalate intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Escalate()
        {
            var escalateMessageVariations = AllResponsesTemplates.ExpandTemplate("EscalateMessage");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Escalate));

            // Assert that card returned is hero card
            Assert.AreEqual(1, responses[2].Attachments.Count);
            Assert.AreEqual("application/vnd.microsoft.card.hero", responses[2].Attachments[0].ContentType);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[3].Text);
        }

        /// <summary>
        /// Assert that Help intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Help()
        {
            var helpMessageVariations = AllResponsesTemplates.ExpandTemplate("HelpCard");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Help));

            // Assert that card returned is hero card
            Assert.AreEqual(1, responses[2].Attachments.Count);
            Assert.AreEqual("application/vnd.microsoft.card.hero", responses[2].Attachments[0].ContentType);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[3].Text);
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

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Logout));

            CollectionAssert.Contains(logoutMessageVariations as ICollection, responses[2].Text);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[3].Text);
        }

        /// <summary>
        /// Assert that Repeat intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_Repeat()
        {
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.Repeat));

            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[2].Text);
        }

        /// <summary>
        /// Assert that StartOver intent is working.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_General_StartOver()
        {
            var startOverMessageVariations = AllResponsesTemplates.ExpandTemplate("StartOverMessage");
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());

            // Returning user card and welcome message represent the first two messages
            Assert.AreEqual(1, responses[0].Attachments.Count);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.StartOver));

            CollectionAssert.Contains(startOverMessageVariations as ICollection, responses[2].Text);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[3].Text);
        }
    }
}