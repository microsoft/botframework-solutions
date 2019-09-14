// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Skills.Auth;
using Microsoft.Bot.Builder.Solutions.Skills.Models;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions.Skills.Tests.Mocks;
using Microsoft.Bot.Builder.Solutions.Skills.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Solutions.Skills.Tests
{
    /// <summary>
    /// Test basic invocation of Skills through the SkillDialog.
    /// </summary>
    [TestClass]
    public class SkillDialogInvocationTests : SkillDialogTestBase
    {
        private readonly IServiceClientCredentials _mockServiceClientCredentials = new MockServiceClientCredentials();
        private readonly MockSkillTransport _mockSkillTransport = new MockSkillTransport();
        private readonly IBotTelemetryClient _mockTelemetryClient = new MockTelemetryClient();
        private SkillManifest _skillManifest;

        [TestInitialize]
        public void AddSkillManifest()
        {
            // Simple skill, no slots
            _skillManifest = ManifestUtilities.CreateSkill(
                "testSkill",
                "testSkill",
                "https://testskill.tempuri.org/api/skill",
                "testSkill/testAction");

            var skillConnectorConfiguration = new SkillConnectionConfiguration()
            {
                SkillManifest = _skillManifest,
                ServiceClientCredentials = _mockServiceClientCredentials,
            };

            // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
            Dialogs.Add(new SkillDialogTest(skillConnectorConfiguration, null, _mockTelemetryClient));
        }

        /// <summary>
        /// Create a SkillDialog and send a message triggering a call to the remote skill through the injected transport.
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
