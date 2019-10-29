// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillSample.Tests.Utterances;

namespace SkillSample.Tests
{
    [TestClass]
    public class InterruptionTests : SkillTestBase
    {
        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReplyOneOf(GetTemplates("NamePrompt"))
               .Send(GeneralUtterances.Help)
               .AssertReply(activity => Assert.AreEqual(1, activity.AsMessageActivity().Attachments.Count))
               .AssertReplyOneOf(GetTemplates("NamePrompt"))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReplyOneOf(GetTemplates("NamePrompt"))
               .Send(GeneralUtterances.Cancel)
               .AssertReplyOneOf(GetTemplates("CancelledMessage"))
               .StartTestAsync();
        }
    }
}
