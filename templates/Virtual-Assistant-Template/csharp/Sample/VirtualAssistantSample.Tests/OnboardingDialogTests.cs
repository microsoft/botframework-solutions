// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using VirtualAssistantSample.Models;
using ActivityGenerator = Microsoft.Bot.Builder.Dialogs.Adaptive.Generators.ActivityGenerator;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    public class OnboardingDialogTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Onboarding_Flow()
        {
            var testName = "Jane Doe";

            UserProfileState profileState = new UserProfileState();
            profileState.Name = testName;

            var allNamePromptVariations = TemplateEngine.ExpandTemplate("NamePrompt");
            var allHaveMessageVariations = TemplateEngine.ExpandTemplate("HaveNameMessage", profileState);

            dynamic data = new JObject();
            data.name = testName;

            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .AssertReplyOneOf(allNamePromptVariations.ToArray())
                .Send(testName)
                .AssertReplyOneOf(allHaveMessageVariations.ToArray())
                .StartTestAsync();
        }
    }
}
