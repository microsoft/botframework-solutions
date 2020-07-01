using System.Collections;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillSample.Tests.Utterances;

namespace SkillSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    [TestCategory("SampleDialog")]
    public class SampleDialogTests : DirectLineClientTestBase
    {
        [TestMethod]
        public async Task Test_Utterance_SampleDialog()
        {
            await Assert_Utterance_Triggers_SkillDialog();
        }

        public async Task Assert_Utterance_Triggers_SkillDialog()
        {
            var profileState = new { Name = TestName };

            var introTextVariations = AllResponsesTemplates.ExpandTemplate("IntroText");
            var firstPromptTextVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptText");
            var namePromptTextVariations = AllResponsesTemplates.ExpandTemplate("NamePromptText");
            var haveNameMessageTextVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessageText", profileState);
            var completedTextVariations = AllResponsesTemplates.ExpandTemplate("CompletedText");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());
            CollectionAssert.Contains(introTextVariations as ICollection, responses[0].Text);
            CollectionAssert.Contains(firstPromptTextVariations as ICollection, responses[1].Text);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(SampleDialogUtterances.Trigger));
            CollectionAssert.Contains(namePromptTextVariations as ICollection, responses[3].Text);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(TestName));
            CollectionAssert.Contains(haveNameMessageTextVariations as ICollection, responses[4].Text);

            CollectionAssert.Contains(completedTextVariations as ICollection, responses[5].Text);
        }
    }
}
