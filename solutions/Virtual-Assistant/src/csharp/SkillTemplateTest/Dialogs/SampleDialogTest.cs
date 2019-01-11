using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SkillTemplateTest.Dialogs
{
    public class SampleDialogTest : SkillDialogTestBase
    {
        [TestMethod]
        public async Task Test_Sample_Dialog()
        {
            await GetTestFlow()
               .Send("demo")
               .AssertReply("send me a message")
               .Send("This is a test")
               .AssertReply("You said: this is a test")
               .StartTestAsync();
        }
    }
}
