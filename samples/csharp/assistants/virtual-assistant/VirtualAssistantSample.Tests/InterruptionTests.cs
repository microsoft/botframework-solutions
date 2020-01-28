// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VirtualAssistantSample.Tests.Utterances;

namespace VirtualAssistantSample.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class InterruptionTests : BotTestBase
    {
        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            var allFirstPromptVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("FirstPromptMessage");

            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(allFirstPromptVariations.ToArray())
                .Send(GeneralUtterances.Help)
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help_Interruption_In_Dialog()
        {
            var allNamePromptVariations = LocaleTemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NamePrompt");

            await GetTestFlow(includeUserProfile: false)
                .Send(string.Empty)
                .AssertReplyOneOf(allNamePromptVariations.ToArray())
                .Send(GeneralUtterances.Help)
                .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
                .AssertReplyOneOf(allNamePromptVariations.ToArray())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            var allFirstPromptVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("FirstPromptMessage");
            var allResponseVariations = TemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("CancelledMessage", TestUserProfileState);

            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(allFirstPromptVariations.ToArray())
                .Send(GeneralUtterances.Cancel)
                .AssertReplyOneOf(allResponseVariations.ToArray())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Repeat_Interruption()
        {
            var allNamePromptVariations = LocaleTemplateEngine.TemplateEnginesPerLocale[CultureInfo.CurrentUICulture.Name].ExpandTemplate("NamePrompt");

            await GetTestFlow(includeUserProfile: false)
                .Send(string.Empty)
                .AssertReplyOneOf(allNamePromptVariations.ToArray())
                .Send(GeneralUtterances.Repeat)
                .AssertReplyOneOf(allNamePromptVariations.ToArray())
                .StartTestAsync();
        }
    }
}
