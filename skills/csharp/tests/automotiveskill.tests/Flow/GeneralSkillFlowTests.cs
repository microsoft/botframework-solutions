// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Threading.Tasks;
using AutomotiveSkill.Responses.Main;
using AutomotiveSkill.Responses.Shared;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutomotiveSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GeneralSkillFlowTests : AutomotiveSkillTestBase
    {
        [TestMethod]
        public async Task Test_SingleTurnCompletion()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("what's the weather?")
                .AssertReplyOneOf(this.ConfusedResponse())
                .StartTestAsync();
        }

        private string[] ConfusedResponse()
        {
            return this.ParseReplies(AutomotiveSkillSharedResponses.DidntUnderstandMessage, new StringDictionary());
        }
    }
}
