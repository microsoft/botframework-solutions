using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.Shared;
using PhoneSkillTest.Flow.Utterances;

namespace PhoneSkillTest.Flow
{
    [TestClass]
    public class MainDialogTests : PhoneSkillTestBase
    {
        [TestMethod]
        public async Task Test_Help_Intent()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Help)
                .AssertReply(Message(PhoneMainResponses.HelpMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Unhandled_Message()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Incomprehensible)
                .AssertReply(Message(PhoneSharedResponses.DidntUnderstandMessage))
                .StartTestAsync();
        }
    }
}
