using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Skills.Tests.Mocks;
using Microsoft.Bot.Builder.Skills.Tests.Utilities;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    /// <summary>
    /// Test basic invocation of Skills that have slots configured and ensure the slots are filled as expected.
    /// </summary>
    [TestClass]
    public class SkillDialogSlotFillingTests : SkillDialogTestBase
    {
        private MockHttpMessageHandler _mockHttp = new MockHttpMessageHandler();
        private List<SkillManifest> _skillManifests = new List<SkillManifest>();

        [TestInitialize]
        public void AddSkills()
        {
            // Simple skill, no slots
            _skillManifests.Add(ManifestUtilities.CreateSkill(
                "testskill", 
                "testskill", 
                "https://testskill.tempuri.org/api/skill",
                "testSkill/testAction"));

            // Simple skill, with one slot (param1)
            var slots = new List<Slot>();
            slots.Add(new Slot("param1", new List<string>() { "string" }));
            _skillManifests.Add(ManifestUtilities.CreateSkill(
                "testskillwithslots", 
                "testskillwithslots",
                "https://testskillwithslots.tempuri.org/api/skill", 
                "testSkill/testActionWithSlots", 
                slots));

            // Each Skill has a number of actions, these actions are added as their own SkillDialog enabling
            // the SkillDialog to know which action is invoked and identify the slots as appropriate.
            foreach (var skill in _skillManifests)
            {
                // Each action within a Skill is registered on it's own as a child of the overall Skill
                foreach (var action in skill.Actions)
                {
                    Dialogs.Add(new SkillDialogTest(skill, action, null, new DummyMicrosoftAppCredentialsEx(null, null, null), null, _mockHttp, UserState));          
                }
            }
        }      

        /// <summary>
        /// Ensure the SkillBegin event activity is sent to the Skill when starting a skill conversation
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SkilllBeginEventTest()
        {
            string eventToMatch = await File.ReadAllTextAsync(@".\TestData\skillBeginEvent.json");

            // When invoking a Skill the first Activity that is sent is skillBegin so we validate this is sent
            // HTTP mock returns "no activities" as per the real scenario and enables the SkillDialog to continue
            _mockHttp.When("https://testskill.tempuri.org/api/skill")
               .With(request=> validateActivity(request, eventToMatch))
               .Respond("application/json", "[]");

            // If the request isn't matched then the event wasn't received as expected
           _mockHttp.Fallback.Throw(new InvalidOperationException("Expected Skill Begin event not found"));

            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            await this.GetTestFlow(_skillManifests.Single(s=>s.Name == "testskill"), "testSkill/testAction", null)
                  .Send("hello")               
                  .StartTestAsync();
        }

        /// <summary>
        /// Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
        /// and present in State.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SkilllBeginEventWithSlotsTest()
        {
            string eventToMatch = await File.ReadAllTextAsync(@".\TestData\skillBeginEventWithOneParam.json");
           
            // When invoking a Skill the first Activity that is sent is skillBegin so we validate this is sent
            // HTTP mock returns "no activities" as per the real scenario and enables the SkillDialog to continue
            _mockHttp.When("https://testskillwithslots.tempuri.org/api/skill")
               .With(request => validateActivity(request, eventToMatch))
               .Respond("application/json", "[]");

            // If the request isn't matched then the event wasn't received as expected
            _mockHttp.Fallback.Throw(new InvalidOperationException("Expected Skill Begin event not found"));

            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Data to add to the UserState managed SkillContext made available for slot filling
            // within SkillDialog
            Dictionary<string, object> slots = new Dictionary<string, object>();
            slots.Add("param1", "TEST");

            await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskillwithslots"), "testSkill/testActionWithSlots", slots)
                  .Send("hello")
                  .StartTestAsync();
        }

        /// <summary>
        /// Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
        /// This test has extra data in the SkillContext "memory" which should not be sent across
        /// and present in State.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SkilllBeginEventWithSlotsTestExtraItems()
        {
            string eventToMatch = await File.ReadAllTextAsync(@".\TestData\skillBeginEventWithOneParam.json");

            // When invoking a Skill the first Activity that is sent is skillBegin so we validate this is sent
            // HTTP mock returns "no activities" as per the real scenario and enables the SkillDialog to continue
            _mockHttp.When("https://testskillwithslots.tempuri.org/api/skill")
               .With(request => validateActivity(request, eventToMatch))
               .Respond("application/json", "[]");

            // If the request isn't matched then the event wasn't received as expected
            _mockHttp.Fallback.Throw(new InvalidOperationException("Expected Skill Begin event not found"));

            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Data to add to the UserState managed SkillContext made available for slot filling
            // within SkillDialog
            Dictionary<string, object> slots = new Dictionary<string, object>();
            slots.Add("param1", "TEST");
            slots.Add("param2", "TEST");
            slots.Add("param3", "TEST");
            slots.Add("param4", "TEST");

            await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskillwithslots"), "testSkill/testActionWithSlots", slots)
                  .Send("hello")
                  .StartTestAsync();
        }

        private bool validateActivity(HttpRequestMessage request, string activityToMatch)
        {
            var activityReceived = request.Content.ReadAsStringAsync().Result;
            return string.Equals(activityReceived, activityToMatch);
        }
    }   
}
