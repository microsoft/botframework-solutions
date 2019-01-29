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
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Setting Temperature to 21°."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_LaneAssistOffConfirmYes()
        {
            await this.GetTestFlow()
                .Send("turn lane assist off")
                .AssertReply(this.CheckReply("Lane Change Alert is an Active Safety feature. Set it to Off? (1) Yes or (2) No"))
                .Send("yes")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Setting Lane Change Alert to Off."))
                .AssertReply(this.CheckForEndOfConversation())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_LaneAssistOffConfirmNo()
        {
            await this.GetTestFlow()
                .Send("turn lane assist off")
                .AssertReply(this.CheckReply("Lane Change Alert is an Active Safety feature. Set it to Off? (1) Yes or (2) No"))
                .Send("no")
                .AssertReply(this.CheckReply("Ok, not making any changes."))
                .AssertReply(this.CheckForEndOfConversation())              
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_WarmUpBackOfCar()
        {
            await this.GetTestFlow()
                .Send("warm up the back of the car")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Increasing Rear Combined Set Temperature."))               
                .StartTestAsync();
        }
        
        [TestMethod]
        public async Task Test_DefogWindscreen()
        {
            await this.GetTestFlow()
                .Send("defog my windshield")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Setting Rear Window Defogger to On."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AirOnFeet()
        {
            await this.GetTestFlow()
                .Send("put the air on my feet")
                .AssertReply(this.CheckReply(" (1) Front Combined Air Delivery Mode Control(2) Rear Combined Air Delivery Mode Control"))
                .Send("front combined air delivery mode control")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Setting Front Combined Air Delivery Mode Control to Floor."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ACOff()
        {
            await this.GetTestFlow()
                .Send("turn off the ac")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Setting Front and Rear HVAC to All Off."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_FeelingCold()
        {
            await this.GetTestFlow()
                .Send("I'm feeling cold")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Increasing Temperature."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_FeelingColdInTheBack()
        {
            await this.GetTestFlow()
                .Send("it's feeling cold in the back")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Increasing Rear Combined Set Temperature."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AdjustEqualizer()
        {
            await this.GetTestFlow()
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply(" (1) Equalizer (Bass)(2) Equalizer (Midrange)(3) Equalizer (Treble)(4) Equalizer (Surround)(5) Air Recirculation"))
                .Send("Equalizer (Bass)")
                .AssertReply(this.CheckReply("Here are the settings for Equalizer (Bass): (1) Decrease(2) Increase"))                
                 .Send("Decrease")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Decreasing Equalizer (Bass)."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithOrdinals()
        {
            await this.GetTestFlow()
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply(" (1) Equalizer (Bass)(2) Equalizer (Midrange)(3) Equalizer (Treble)(4) Equalizer (Surround)(5) Air Recirculation"))
                .Send("first one")
                .AssertReply(this.CheckReply("Here are the settings for Equalizer (Bass): (1) Decrease(2) Increase"))
                .Send("second one")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Increasing Equalizer (Bass)."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_IncorrectValueChoice()
        {
            await this.GetTestFlow()
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply(" (1) Equalizer (Bass)(2) Equalizer (Midrange)(3) Equalizer (Treble)(4) Equalizer (Surround)(5) Air Recirculation"))
                .Send("first one")
                .AssertReply(this.CheckReply("Here are the settings for Equalizer (Bass): (1) Decrease(2) Increase"))
                .Send("blah blah")
                .AssertReply(this.CheckReply("Here are the settings for Equalizer (Bass): (1) Decrease(2) Increase"))
                .Send("Decrease")
                .AssertReply(this.CheckForSettingEvent())
                .AssertReply(this.CheckReply("Decreasing Equalizer (Bass)."))
                .StartTestAsync();
        }

        private Action<IActivity> CheckForSettingEvent()
        {
            return activity =>
            {
                var eventReceived = activity.AsEventActivity();
                Assert.IsNotNull(eventReceived,"Activity received is not an Event as expected");             
            };
        }

        private Action<IActivity> CheckForEndOfConversation()
        {
            return activity =>
            {
                var eventReceived = activity.AsEndOfConversationActivity();
                Assert.IsNotNull(eventReceived, "End of Conversation Activity not received.");
            };
        }      

        private Action<IActivity> CheckReply(string expectedResponse)
        {
            return activity =>
            {
                var messageReceived = activity.AsMessageActivity();
                Assert.IsNotNull(messageReceived, "Activity received is not of type Message");
                Assert.AreEqual<string>(expectedResponse, messageReceived.Text);
            };
        }
    }
}
