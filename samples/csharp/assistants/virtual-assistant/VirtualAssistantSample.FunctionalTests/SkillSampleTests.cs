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
    [TestCategory("SkillSample")]
    public class SkillSampleTests : DirectLineClientTestBase
    {
        [TestMethod]
        public async Task Test_Sample_Utterance()
        {
            await Assert_New_User_Greeting();
            await Assert_Utterance_Triggers_SkillSample();
        }

        /// <summary>
        /// Assert that a new user is greeted with the onboarding prompt. 
        /// </summary>
        /// <param name="useComplexResponseWithName">Send user name only or included in "My name is X" message.</param>
        /// <returns>Task.</returns>
        public async Task Assert_New_User_Greeting(bool useComplexResponseWithName = true)
        {
            var profileState = new UserProfileState { Name = TestName };
            var namePromptVariations = AllResponsesTemplates.ExpandTemplate("NamePrompt");
            var haveNameMessageVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessage", profileState);

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());
            Assert.AreEqual(1, responses[0].Attachments.Count);
            CollectionAssert.Contains(namePromptVariations as ICollection, responses[1].Text);

            if (useComplexResponseWithName)
            {
                var myNameIsMessage = $"My name is {TestName}";
                responses = await SendActivityAsync(conversation, CreateMessageActivity(myNameIsMessage));

                CollectionAssert.Contains(haveNameMessageVariations as ICollection, responses[2].Text);
            }
            else
            {
                responses = await SendActivityAsync(conversation, CreateMessageActivity(TestName));

                CollectionAssert.Contains(haveNameMessageVariations as ICollection, responses[2].Text);
            }
        }

        /// <summary>
        /// Assert that a connected SKillSample is triggered by a sample utterance and completes the VA dialog.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Assert_Utterance_Triggers_SkillSample()
        {
            var profileState = new UserProfileState { Name = TestName };
            var namePromptVariations = AllResponsesTemplates.ExpandTemplate("NamePrompt");
            var haveNameMessageVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessage", profileState);
            var firstPromptVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptMessage");
            var completedMessageVariations = AllResponsesTemplates.ExpandTemplate("CompletedMessage");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());
            Assert.AreEqual(1, responses[0].Attachments.Count);
            CollectionAssert.Contains(firstPromptVariations as ICollection, responses[1].Text);

            // Assert Skill is triggered by sample utterance
            responses = await SendActivityAsync(conversation, CreateMessageActivity(GeneralUtterances.SkillSample));
            CollectionAssert.Contains(namePromptVariations as ICollection, responses[2].Text);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(TestName));
            CollectionAssert.Contains(haveNameMessageVariations as ICollection, responses[3].Text);

            // Assert dialog has completed
            CollectionAssert.Contains(completedMessageVariations as ICollection, responses[4].Text);
        }
    }
}