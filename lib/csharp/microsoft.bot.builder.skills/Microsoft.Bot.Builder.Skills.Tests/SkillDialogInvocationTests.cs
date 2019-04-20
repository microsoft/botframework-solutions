using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Skills.Tests.Mocks;
using Microsoft.Bot.Builder.Skills.Tests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;

namespace Microsoft.Bot.Builder.Skills.Tests
{
    /// <summary>
    /// Test basic invocation of Skills through the SkillDialog.
    /// </summary>
    [TestClass]
    public class SkillDialogInvocationTests : SkillDialogTestBase
    {
        private SkillManifest _skillManifest;
        private MockHttpMessageHandler _mockHttp = new MockHttpMessageHandler();
        private MockTelemetryClient _mockTelemetryClient = new MockTelemetryClient();

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
            Dialogs.Add(new SkillDialogTest(_skillManifest, null, new DummyMicrosoftAppCredentialsEx(null, null, null), _mockTelemetryClient, _mockHttp, UserState));
        }

        /// <summary>
        /// Create a SkillDialog and send a mesage triggering a HTTP call to the remote skill which the mock intercepts.
        /// This ensures the SkillDialog is handling the SkillManifest correctly and sending the HttpRequest to the skill
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task InvokeSkillDialog()
        {
            // When invoking a Skill the first Activity that is sent is skillBegin so we validate this is sent
            // HTTP mock returns "no activities" as per the real scenario and enables the SkillDialog to continue
            _mockHttp.When("https://testskill.tempuri.org/api/skill")
               .Respond("application/json", "[]");

            await this.GetTestFlow(_skillManifest, "testSkill/testAction", null)
                  .Send("hello")
                  .StartTestAsync();

            try
            {
                // Check if a request was sent to the mock, if not the test has failed (skill wasn't invoked).
                _mockHttp.VerifyNoOutstandingRequest();
            }
            catch (InvalidOperationException)
            {
                Assert.Fail("The SkillDialog didn't post an Activity to the HTTP endpoint as expected");
            }
        }
    }
}