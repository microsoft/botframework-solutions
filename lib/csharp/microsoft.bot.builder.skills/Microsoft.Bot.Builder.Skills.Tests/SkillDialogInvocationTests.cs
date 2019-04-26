using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Skills.Tests.Mocks;
using Microsoft.Bot.Builder.Skills.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    /// <summary>
    /// Test basic invocation of Skills through the SkillDialog.
    /// </summary>
    [TestClass]
    public class SkillDialogInvocationTests : SkillDialogTestBase
    {
        private SkillManifest _skillManifest;
        private IBotTelemetryClient _mockTelemetryClient = new MockTelemetryClient();
		private MockSkillTransport _mockSkillTransport = new MockSkillTransport();
		private IServiceClientCredentials _mockServiceClientCredentials = new MockServiceClientCredentials();

        [TestInitialize]
        public void AddSkillManifest()
        {
            // Simple skill, no slots
            _skillManifest = ManifestUtilities.CreateSkill(
                "testSkill",
                "testSkill",
                "https://testskill.tempuri.org/api/skill",
                "testSkill/testAction");

			// Add the SkillDialog to the available dialogs passing the initialized FakeSkill
			Dialogs.Add(new SkillDialogTest(
				_skillManifest,
				_mockServiceClientCredentials,
				_mockTelemetryClient,
				UserState,
				_mockSkillTransport));
        }

        /// <summary>
        /// Create a SkillDialog and send a mesage triggering a call to the remote skill through the injected transport.
        /// This ensures the SkillDialog is handling the SkillManifest and calling the skill correctly.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task InvokeSkillDialog()
        {
            await this.GetTestFlow(_skillManifest, "testSkill/testAction", null)
                  .Send("hello")
                  .StartTestAsync();

            try
            {
                // Check if a request was sent to the mock, if not the test has failed (skill wasn't invoked).
                Assert.IsTrue(_mockSkillTransport.CheckIfSkillInvoked());
            }
            catch
            {
                Assert.Fail("The SkillDialog didn't invoke the skill as expected");
            }
        }
    }
}