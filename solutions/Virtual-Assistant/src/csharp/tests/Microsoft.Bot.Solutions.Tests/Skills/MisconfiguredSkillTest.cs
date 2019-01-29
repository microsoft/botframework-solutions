using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Tests.Skills.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Tests.Skills
{
    [TestClass]
    public class MisconfiguredSkillTest : SkillTestBase
    {
        [TestInitialize]
        public void InitSkills()
        {
            // Add Fake Skill registration
            const string fakeSkillName = "FakeSkill";
            var fakeSkillDefinition = new SkillDefinition();
            var fakeSkillType = typeof(FakeSkill.FakeSkill);
            fakeSkillDefinition.Id = fakeSkillName;
            fakeSkillDefinition.Name = fakeSkillName;

            // Set Assembly name to invalid value
            fakeSkillDefinition.Assembly = "FakeSkill.FakeSkil, Microsoft.Bot.Solutions.Tests, Version = 1.0.0.0, Culture = neutral, PublicKeyToken = null";

            SkillConfigurations.Add(fakeSkillDefinition.Id, Services);

            // Options are passed to the SkillDialog
            SkillDialogOptions = new SkillDialogOptions();
            SkillDialogOptions.SkillDefinition = fakeSkillDefinition;

            // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
            Dialogs.Add(new SkillDialog(fakeSkillDefinition, Services, null, TelemetryClient));
        }

        /// <summary>
        /// Validate that Skill instantiation errors are surfaced as exceptions
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [ExpectedException(typeof(System.InvalidOperationException), "Skill (FakeSkill) could not be created.")]
        public async Task MisconfiguredSkill()
        {         
            await GetTestFlow()
              .Send(SampleDialogUtterances.Trigger)
              .StartTestAsync();
        }
    }
}
