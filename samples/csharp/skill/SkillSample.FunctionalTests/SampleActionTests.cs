using System.Collections;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using SkillSample.Tests.Utterances;

namespace SkillSample.FunctionalTests
{
    [TestClass]
    [TestCategory("FunctionalTests")]
    [TestCategory("SampleAction")]
    public class SampleActionTests : DirectLineClientTestBase
    {
        [TestMethod]
        public async Task Test_Action_SampleAction()
        {
            await Assert_Action_Triggers_SkillAction();
        }

        [TestMethod]
        public async Task Test_ActionWithInput_SampleAction()
        {
            await Assert_ActionWithInput_Triggers_SkillAction();
        }

        public async Task Assert_Action_Triggers_SkillAction()
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

            responses = await SendActivityAsync(conversation, CreateEventActivity(SampleDialogUtterances.EventName));
            CollectionAssert.Contains(namePromptTextVariations as ICollection, responses[3].Text);

            responses = await SendActivityAsync(conversation, CreateMessageActivity(TestName));
            CollectionAssert.Contains(haveNameMessageTextVariations as ICollection, responses[4].Text);

            CollectionAssert.Contains(completedTextVariations as ICollection, responses[5].Text);
        }

        public async Task Assert_ActionWithInput_Triggers_SkillAction()
        {
            var profileState = new { Name = TestName };

            var introTextVariations = AllResponsesTemplates.ExpandTemplate("IntroText");
            var firstPromptTextVariations = AllResponsesTemplates.ExpandTemplate("FirstPromptText");
            var haveNameMessageTextVariations = AllResponsesTemplates.ExpandTemplate("HaveNameMessageText", profileState);
            var completedTextVariations = AllResponsesTemplates.ExpandTemplate("CompletedText");

            var conversation = await StartBotConversationAsync();

            var responses = await SendActivityAsync(conversation, CreateStartConversationEvent());
            CollectionAssert.Contains(introTextVariations as ICollection, responses[0].Text);
            CollectionAssert.Contains(firstPromptTextVariations as ICollection, responses[1].Text);

            responses = await SendActivityAsync(conversation, CreateEventActivity(SampleDialogUtterances.EventName, JObject.FromObject(profileState)));
            CollectionAssert.Contains(haveNameMessageTextVariations as ICollection, responses[3].Text);
            CollectionAssert.Contains(completedTextVariations as ICollection, responses[4].Text);
        }
    }
}
