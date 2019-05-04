using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Skills.Tests.Mocks;
using Microsoft.Bot.Builder.Skills.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    /// <summary>
    /// Test basic invocation of Skills that have slots configured and ensure the slots are filled as expected.
    /// </summary>
    [TestClass]
    public class SkillDialogSlotFillingTests : SkillDialogTestBase
    {
        private List<SkillManifest> _skillManifests = new List<SkillManifest>();
        private IBotTelemetryClient _mockTelemetryClient = new MockTelemetryClient();
		private MockSkillTransport _mockSkillTransport = new MockSkillTransport();
		private IServiceClientCredentials _mockServiceClientCredentials = new MockServiceClientCredentials();

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

            // Simple skill, with two actions and multiple slots
            var multiParamSlots = new List<Slot>();
            multiParamSlots.Add(new Slot("param1", new List<string>() { "string" }));
            multiParamSlots.Add(new Slot("param2", new List<string>() { "string" }));
            multiParamSlots.Add(new Slot("param3", new List<string>() { "string" }));

            var multiActionSkill = ManifestUtilities.CreateSkill(
                "testskillwithmultipleactionsandslots",
                "testskillwithmultipleactionsandslots",
                "https://testskillwithslots.tempuri.org/api/skill",
                "testSkill/testAction1",
                multiParamSlots);

            multiActionSkill.Actions.Add(ManifestUtilities.CreateAction("testSkill/testAction2", multiParamSlots));
            _skillManifests.Add(multiActionSkill);

            // Each Skill has a number of actions, these actions are added as their own SkillDialog enabling
            // the SkillDialog to know which action is invoked and identify the slots as appropriate.
            foreach (var skill in _skillManifests)
            {
                Dialogs.Add(new SkillDialogTest(skill, _mockServiceClientCredentials, _mockTelemetryClient, UserState, _mockSkillTransport));
            }
        }

        /// <summary>
        /// Ensure the SkillBegin event activity is sent to the Skill when starting a skill conversation.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task SkilllBeginEventTest()
        {
            string eventToMatch = await File.ReadAllTextAsync(@".\TestData\skillBeginEvent.json");

            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskill"), "testSkill/testAction", null)
                  .Send("hello")
                  .StartTestAsync();

			_mockSkillTransport.VerifyActivityForwardedCorrectly(activity => ValidateActivity(activity, eventToMatch));
        }

        /// <summary>
        /// Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
        /// and present in State.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task SkilllBeginEventWithSlotsTest()
        {
            string eventToMatch = await File.ReadAllTextAsync(@".\TestData\skillBeginEventWithOneParam.json");

            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Data to add to the UserState managed SkillContext made available for slot filling
            // within SkillDialog
            Dictionary<string, object> slots = new Dictionary<string, object>();
            slots.Add("param1", "TEST");

            await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskillwithslots"), "testSkill/testActionWithSlots", slots)
                  .Send("hello")
                  .StartTestAsync();

			_mockSkillTransport.VerifyActivityForwardedCorrectly(activity => ValidateActivity(activity, eventToMatch));
		}

		/// <summary>
		/// Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
		/// This test has extra data in the SkillContext "memory" which should not be sent across
		/// and present in State.
		/// </summary>
		/// <returns>Task.</returns>
		[TestMethod]
        public async Task SkilllBeginEventWithSlotsTestExtraItems()
        {
            string eventToMatch = await File.ReadAllTextAsync(@".\TestData\skillBeginEventWithOneParam.json");

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

			_mockSkillTransport.VerifyActivityForwardedCorrectly(activity => ValidateActivity(activity, eventToMatch));
		}

        /// <summary>
        /// Ensure the skillBegin event is sent and includes the slots that were configured in the manifest
        /// and present in State. This doesn't pass an action so "global" slot filling is used
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task SkilllBeginEventNoActionPassed()
        {
            string eventToMatch = await File.ReadAllTextAsync(@".\TestData\skillBeginEventWithTwoParams.json");

            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

            // Data to add to the UserState managed SkillContext made available for slot filling
            // within SkillDialog
            Dictionary<string, object> slots = new Dictionary<string, object>();
            slots.Add("param1", "TEST");
            slots.Add("param2", "TEST2");

            // Not passing action to test the "global" slot filling behaviour
            await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskillwithmultipleactionsandslots"), null, slots)
                  .Send("hello")
                  .StartTestAsync();

            _mockSkillTransport.VerifyActivityForwardedCorrectly(activity => ValidateActivity(activity, eventToMatch));
        }

        private bool ValidateActivity(string activitySent, string activityToMatch)
        {
            return string.Equals(activitySent, activityToMatch);
        }
    }
}