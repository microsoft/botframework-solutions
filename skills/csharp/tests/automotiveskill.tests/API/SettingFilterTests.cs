// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AutomotiveSkill.Dialogs;
using AutomotiveSkill.Models;
using AutomotiveSkill.Tests.Flow;
using AutomotiveSkill.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AutomotiveSkill.Tests.API
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class SettingFilterTests : AutomotiveSkillTestBase
    {
        private SettingList settingList;
        private SettingFilter settingFilter;

        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();

            // Supporting setting files are stored as embedded resources
            var resourceAssembly = typeof(VehicleSettingsDialog).Assembly;

            var settingFile = resourceAssembly
                .GetManifestResourceNames()
                .Where(x => x.Contains("available_settings.yaml"))
                .First();

            var alternativeSettingFileName = resourceAssembly
                .GetManifestResourceNames()
                .Where(x => x.Contains("setting_alternative_names.yaml"))
                .First();

            settingList = new SettingList(resourceAssembly, settingFile, alternativeSettingFileName);
            settingFilter = new SettingFilter(settingList);
        }

        [TestMethod]
        public void Test_SettingFilter_Temperature()
        {
            var state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "temperature" });
            state.Entities.Add("AMOUNT", new List<string> { "21" });
            state.Entities.Add("UNIT", new List<string> { "degrees" });

            settingFilter.PostProcessSettingName(state);

            Assert.AreEqual("Temperature", state.Changes[0].SettingName);
            Assert.AreEqual(21, state.Changes[0].Amount.Amount);
        }

        [TestMethod]
        public void Test_SettingFilter_Defog()
        {
            var state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "defog" });

            settingFilter.PostProcessSettingName(state);

            Assert.AreEqual("Rear Window Defogger", state.Changes[0].SettingName);
            Assert.AreEqual("On", state.Changes[0].Value);
        }

        [TestMethod]
        public void Test_SettingFilter_ColdInTheBack()
        {
            var state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "back" });
            state.Entities.Add("VALUE", new List<string> { "cold" });

            settingFilter.PostProcessSettingName(state, true);

            Assert.AreEqual("Rear Combined Set Temperature", state.Changes[0].SettingName);
            Assert.AreEqual("Increase", state.Changes[0].Value);
        }

        [TestMethod]
        public void Test_SettingFilter_PassengerFeelingCold()
        {
            var state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "passenger" });
            state.Entities.Add("VALUE", new List<string> { "cold" });

            settingFilter.PostProcessSettingName(state, true);

            Assert.AreEqual("Front Right Set Temperature", state.Changes[0].SettingName);
            Assert.AreEqual("Increase", state.Changes[0].Value);
        }

        [TestMethod]
        public void Test_SettingFilter_FeetFeelingCold()
        {
            var state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "feet" });
            state.Entities.Add("VALUE", new List<string> { "cold" });

            settingFilter.PostProcessSettingName(state, true);

            Assert.AreEqual("Temperature", state.Changes[0].SettingName);
            Assert.AreEqual("Increase", state.Changes[0].Value);
        }
    }
}