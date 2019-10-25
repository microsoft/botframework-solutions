﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using ITSMSkill.Responses.Main;
using ITSMSkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ITSMSkill.Tests.Flow
{
    [TestClass]
    public class GeneralFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task HelpTest()
        {
            await this.GetTestFlow()
                .Send(GeneralTestUtterances.Help)
                .AssertReply(AssertContains(MainResponses.HelpMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CancelTest()
        {
            await this.GetTestFlow()
                .Send(GeneralTestUtterances.Cancel)
                .AssertReply(AssertContains(MainResponses.CancelMessage))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
