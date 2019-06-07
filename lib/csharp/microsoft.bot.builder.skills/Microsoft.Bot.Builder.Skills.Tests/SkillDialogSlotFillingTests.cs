using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Skills.Tests.Mocks;
using Microsoft.Bot.Builder.Skills.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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
            slots.Add(new Slot { Name = "param1", Types = new List<string>() { "string" } });
            _skillManifests.Add(ManifestUtilities.CreateSkill(
                "testskillwithslots",
                "testskillwithslots",
                "https://testskillwithslots.tempuri.org/api/skill",
                "testSkill/testActionWithSlots",
                slots));

            // Simple skill, with two actions and multiple slots
            var multiParamSlots = new List<Slot>();
            multiParamSlots.Add(new Slot { Name = "param1", Types = new List<string>() { "string" } });
            multiParamSlots.Add(new Slot { Name = "param2", Types = new List<string>() { "string" } });
            multiParamSlots.Add(new Slot { Name = "param3", Types = new List<string>() { "string" } });

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
        /// Ensure the activity received on the skill side includes the slots that were configured in the manifest.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task SkillInvocationWithSlotsTest()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

			var slots = new SkillContext();
			dynamic entity = new { key1 = "TEST1", key2 = "TEST2" };
			slots.Add("param1", JObject.FromObject(entity));

			await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskillwithslots"), "testSkill/testActionWithSlots", slots)
                  .Send("hello")
                  .StartTestAsync();

			_mockSkillTransport.VerifyActivityForwardedCorrectly(activity =>
			{
				var semanticAction = activity.SemanticAction;
				Assert.AreEqual(semanticAction.Entities["param1"].Properties["key1"], "TEST1");
				Assert.AreEqual(semanticAction.Entities["param1"].Properties["key2"], "TEST2");
			});
		}

		/// <summary>
		/// Ensure the activity received on the skill side includes the slots that were configured in the manifest
		/// This test has extra data in the SkillContext "memory" which should not be sent across.
		/// </summary>
		/// <returns>Task.</returns>
		[TestMethod]
        public async Task SkillInvocationWithSlotsTestExtraItems()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

			var slots = new SkillContext();
			dynamic entity = new { key1 = "TEST1", key2 = "TEST2" };
			slots.Add("param1", JObject.FromObject(entity));

			await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskillwithslots"), "testSkill/testActionWithSlots", slots)
                  .Send("hello")
                  .StartTestAsync();

			_mockSkillTransport.VerifyActivityForwardedCorrectly(activity =>
			{
				var semanticAction = activity.SemanticAction;
				Assert.AreEqual(semanticAction.Entities["param1"].Properties["key1"], "TEST1");
				Assert.AreEqual(semanticAction.Entities["param1"].Properties["key2"], "TEST2");
			});
		}

		/// <summary>
		/// Ensure the activity received on the skill side includes the slots that were configured in the manifest
		/// This doesn't pass an action so "global" slot filling is used.
		/// </summary>
		/// <returns>Task.</returns>
		[TestMethod]
        public async Task SkillInvocationNoActionPassed()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();

			var slots = new SkillContext();
			dynamic entity = new { key1 = "TEST1", key2 = "TEST2" };
			slots.Add("param1", JObject.FromObject(entity));

            // Not passing action to test the "global" slot filling behaviour
            await this.GetTestFlow(_skillManifests.Single(s => s.Name == "testskillwithmultipleactionsandslots"), null, slots)
                  .Send("hello")
                  .StartTestAsync();

            _mockSkillTransport.VerifyActivityForwardedCorrectly(activity =>
			{
				var semanticAction = activity.SemanticAction;
				Assert.AreEqual(semanticAction.Entities["param1"].Properties["key1"], "TEST1");
				Assert.AreEqual(semanticAction.Entities["param1"].Properties["key2"], "TEST2");
			});
        }
    }
}