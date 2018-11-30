using AutomotiveSkillTest.Flow;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace AutomotiveSkillTest.Flow
{
    [TestClass]
    public class VehicleSettingsTests: AutomotiveSkillTestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            base.Initialize();
        }

        [TestMethod]
        public async Task Test_SettingTemperature()
        {
            await this.GetTestFlow()
                .Send("set temperature to 21 degrees")                
                .AssertReply(this.CheckForEvent())
                .AssertReply(this.CheckForSettingReply())               
                .StartTestAsync();
        }

        private Action<IActivity> CheckForEvent()
        {
            return activity =>
            {
                var eventReceived = activity.AsEventActivity();
                Assert.IsNotNull(eventReceived);             
            };
        }

        private Action<IActivity> CheckForSettingReply()
        {
            return activity =>
            {
                var messageReceived = activity.AsMessageActivity();
                Assert.IsNotNull(messageReceived);
                Assert.AreEqual<string>(messageReceived.Text, "Setting Temperature to 21°.");
            };
        }
    }
}
