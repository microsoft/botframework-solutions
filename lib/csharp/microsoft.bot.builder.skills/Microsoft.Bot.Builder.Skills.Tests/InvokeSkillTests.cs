using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Tests.Skills
{
    [TestClass]
    public class InvokeSkillTests : SkillTestBase
    {
        SkillDefinition _skillDefinition;
        private string locationEvent = "/event:{ \"Name\": \"IPA.Location\", \"Value\": \"47.639620,-122.130610\" }";

        [TestInitialize]
        public void InitSkills()
        {
            _skillDefinition = new SkillDefinition();
            _skillDefinition.Name = "pointOfInterestSkill";
            _skillDefinition.Endpoint = "https://djremotepoiskill.azurewebsites.net/api/skill";

            // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
            Dialogs.Add(new SkillDialog(_skillDefinition, null));        
        }

        /// <summary>
        /// Test that we can initiate a conversation with a remote skill through the SkillDialog/SkillAdapter
        /// signalling the Skill has completed.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InvokeRemotePoIAkSkill()
        {
            await GetTestFlow(_skillDefinition)
               .Send(locationEvent)
               .Send("find a coffee shop")        
               .AssertReply(PoIResultPrompt())
               .StartTestAsync();
        }

        private Action<IActivity> PoIResultPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.IsNotNull(messageActivity, $"Expected a message response but received a {activity.Type.ToString()} instead.");

                // Retrieve the main part of the response which is before the line-break
                int index = messageActivity.Text.IndexOf("\n");
                if (index > 0)
                    messageActivity.Text = messageActivity.Text.Substring(0, index);

                CollectionAssert.Contains(
                    new string[] { "Where would you like to go?", "Which would you like?" },
                    messageActivity.Text,
                    $"Expected a PoI results prompt but got this instead: {messageActivity.Text}"
                    );
            };
        }
    }
}