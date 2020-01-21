﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillSample.Tests.Utterances;

namespace SkillSample.Tests
{
    [TestClass]
    public class SampleDialogTests : SkillTestBase
    {
        [TestMethod]
        public async Task Test_Sample_Dialog()
        {
            await GetTestFlow()
               .Send(SampleDialogUtterances.Trigger)
               .AssertReplyOneOf(GetTemplates("NamePromptText"))
               .Send(SampleDialogUtterances.NamePromptResponse)
               .AssertReplyOneOf(GetTemplates("HaveNameMessageText", new { Name = SampleDialogUtterances.NamePromptResponse }))
               .StartTestAsync();
        }
    }
}
