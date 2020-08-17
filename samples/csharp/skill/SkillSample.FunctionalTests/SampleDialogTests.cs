// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillSample.FunctionalTests.Bot;
using SkillSample.FunctionalTests.Configuration;
using SkillSample.Tests;
using SkillSample.Tests.Utterances;

namespace SkillSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    [TestCategory("SampleDialog")]
    public class SampleDialogTests : SkillTestBase
    {
        [TestMethod]
        public async Task Test_Utterance_SampleDialog()
        {
            var profileState = new { Name = SampleDialogUtterances.NamePromptResponse };
            var introTextVariations = AllResponsesTemplates.ExpandTemplate("IntroText");
            var namePromptTextVariations = AllResponsesTemplates.ExpandTemplate("NamePromptText");
            var haveNameMessageTextVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessageText", profileState);

            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var testBot = new TestBotClient(new EnvironmentBotTestConfiguration());

            await testBot.StartConversationAsync(cancellationTokenSource.Token);
            await testBot.SendEventAsync("startConversation", cancellationTokenSource.Token);
            await testBot.AssertReplyOneOfAsync(introTextVariations, cancellationTokenSource.Token);
            await testBot.SendMessageAsync(SampleDialogUtterances.Trigger);
            await testBot.AssertReplyOneOfAsync(namePromptTextVariations, cancellationTokenSource.Token);
            await testBot.SendMessageAsync(SampleDialogUtterances.NamePromptResponse);
            await testBot.AssertReplyOneOfAsync(haveNameMessageTextVariations, cancellationTokenSource.Token);
        }
    }
}
