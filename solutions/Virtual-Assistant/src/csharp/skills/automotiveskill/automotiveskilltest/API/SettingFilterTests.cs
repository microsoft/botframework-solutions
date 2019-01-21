using AutomotiveSkill;
using AutomotiveSkill.Common;
using AutomotiveSkillTest.Flow;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace AutomotiveSkillTest.API
{
    [TestClass]
    public class SettingFilterTests : AutomotiveSkillTestBase
    {
        private SettingList settingList;
        private SettingFilter settingFilter;

        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();

            var resDir = Path.Combine(Path.GetDirectoryName(typeof(SettingFilterTests).Assembly.Location), "Dialogs\\VehicleSettings\\Resources\\");

            settingList = new SettingList(resDir + "available_settings.json", resDir + "setting_alternative_names.json");
            settingFilter = new SettingFilter(settingList);

        }

        [TestMethod]
        public void Test_SettingFilter_Temperature()
        {
            AutomotiveSkillState state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string>{ "temperature"});
            state.Entities.Add("AMOUNT", new List<string> { "21" });
            state.Entities.Add("UNIT", new List<string> { "degrees" });

            settingFilter.PostProcessSettingName(state);

            Assert.AreEqual("Temperature", state.Changes[0].SettingName);
            Assert.AreEqual(21, state.Changes[0].Amount.Amount);
        }

        [TestMethod]
        public void Test_SettingFilter_Defog()
        {
            AutomotiveSkillState state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "defog" });

            settingFilter.PostProcessSettingName(state);

            Assert.AreEqual("Rear Window Defogger", state.Changes[0].SettingName);
            Assert.AreEqual("On", state.Changes[0].Value);
        }

        [TestMethod]
        public void Test_SettingFilter_ColdInTheBack()
        {
            AutomotiveSkillState state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "back" });
            state.Entities.Add("VALUE", new List<string> { "cold" });

            settingFilter.PostProcessSettingName(state,true);

            Assert.AreEqual("Rear Combined Set Temperature", state.Changes[0].SettingName);
            Assert.AreEqual("Increase", state.Changes[0].Value);
        }

        [TestMethod]
        public void Test_SettingFilter_PassengerFeelingCold()
        {
            AutomotiveSkillState state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "passenger" });
            state.Entities.Add("VALUE", new List<string> { "cold" });

            settingFilter.PostProcessSettingName(state,true);

            Assert.AreEqual("Front Right Set Temperature", state.Changes[0].SettingName);
            Assert.AreEqual("Increase", state.Changes[0].Value);
        }

        [TestMethod]
        public void Test_SettingFilter_FeetFeelingCold()
        {
            AutomotiveSkillState state = new AutomotiveSkillState();
            state.Entities.Add("SETTING", new List<string> { "feet" });
            state.Entities.Add("VALUE", new List<string> { "cold" });

            settingFilter.PostProcessSettingName(state,true);

            Assert.AreEqual("Temperature", state.Changes[0].SettingName);
            Assert.AreEqual("Increase", state.Changes[0].Value);
        }
    }
}