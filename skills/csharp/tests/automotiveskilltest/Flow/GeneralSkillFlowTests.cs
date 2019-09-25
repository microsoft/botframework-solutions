using System.Collections.Specialized;
using System.Threading.Tasks;
using AutomotiveSkill.Responses.Shared;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutomotiveSkill.Tests.Flow
{
    [TestClass]
    public class GeneralSkillFlowTests : AutomotiveSkill.TestsBase
    {
        [TestMethod]
        public async Task Test_SingleTurnCompletion()
        {
            await this.GetTestFlow()
                .Send("what's the weather?")
                .AssertReplyOneOf(this.ConfusedResponse())
                .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.Handoff, activity.Type); })
                .StartTestAsync();
        }

        private string[] ConfusedResponse()
        {
            return this.ParseReplies(AutomotiveSkillSharedResponses.DidntUnderstandMessage, new StringDictionary());
        }
    }
}
