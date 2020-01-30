// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Responses.Shared;
using PhoneSkill.Tests.Flow.Utterances;

namespace PhoneSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GeneralSkillFlowTests : PhoneSkillTestBase
    {
        [TestMethod]
        public async Task Test_SingleTurnCompletion()
        {
            await this.GetTestFlow()
                .Send(GeneralUtterances.Incomprehensible)
                .AssertReplyOneOf(this.ConfusedResponse())
                .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.EndOfConversation, activity.Type); })
                .StartTestAsync();
        }

        private string[] ConfusedResponse()
        {
            return this.ParseReplies(PhoneSharedResponses.DidntUnderstandMessage, new StringDictionary());
        }
    }
}
