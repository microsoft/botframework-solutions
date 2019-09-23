﻿using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Responses.Shared;
using PhoneSkillTest.Flow.Utterances;

namespace PhoneSkillTest.Flow
{
    [TestClass]
    public class GeneralSkillFlowTests : PhoneSkillTestBase
    {
        [TestMethod]
        public async Task Test_SingleTurnCompletion()
        {
            await this.GetTestFlow()
                .Send(GeneralUtterances.Incomprehensible)
                .AssertReplyOneOf(this.ConfusedResponse())
                .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.Handoff, activity.Type); })
                .StartTestAsync();
        }

        private string[] ConfusedResponse()
        {
            return this.ParseReplies(PhoneSharedResponses.DidntUnderstandMessage, new StringDictionary());
        }
    }
}
