using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.OutgoingCall;
using PhoneSkill.Tests.Flow.Utterances;

namespace PhoneSkill.Tests.Flow
{
    [TestClass]
    public class InterruptionTests : PhoneSkillTestBase
    {
        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            await GetTestFlow()
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities)
               .AssertReply(ShowAuth())
               .Send(GetAuthResponse())
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(GeneralUtterances.Help)
               .AssertReply(Message(PhoneMainResponses.HelpMessage))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            await GetTestFlow()
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities)
               .AssertReply(ShowAuth())
               .Send(GetAuthResponse())
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(GeneralUtterances.Cancel)
               .AssertReply(Message(PhoneMainResponses.CancelMessage))
               .StartTestAsync();
        }
    }
}
