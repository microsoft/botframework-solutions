using AutomotiveSkill.Models;
using AutomotiveSkillTest.Flow;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Temperature",
                    Value = "Set",
                    Amount = new SettingAmount()
                    {
                        Amount = 21,
                        Unit = "°",
                    },
                }))
                .AssertReply(this.CheckReply("Setting Temperature to 21°."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_IncreaseTemperatureByRelativeAmount()
        {
            await this.GetTestFlow()
                .Send("increase temperature by 2")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Temperature",
                    Value = "Increase",
                    Amount = new SettingAmount()
                    {
                        Amount = 2,
                    },
                    IsRelativeAmount = true,
                }))
                .AssertReply(this.CheckReply("Increasing Temperature by 2."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_IncreaseTemperatureToAbsoluteAmount()
        {
            await this.GetTestFlow()
                .Send("increase temperature to 24")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Temperature",
                    Value = "Increase",
                    Amount = new SettingAmount()
                    {
                        Amount = 24,
                    },
                }))
                .AssertReply(this.CheckReply("Setting Temperature to 24."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingTemperatureMissingValue()
        {
            await this.GetTestFlow()
                .Send("change the temperature")
                 .AssertReply(this.CheckReply("Here are the possible values for Temperature. Which one? (1) Decrease(2) Increase"))
                .Send("Increase")                
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Increasing Temperature."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_LaneAssistOffConfirmYes()
        {
            await this.GetTestFlow()
                .Send("turn lane assist off")
                .AssertReply(this.CheckReply("So, you want to change Lane Change Detection to Off. Is that correct? (1) Yes or (2) No"))
                .Send("yes")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Lane Change Detection",
                    Value = "Off",
                    IsConfirmed = true,
                }))
                .AssertReply(this.CheckReply("Setting Lane Change Detection to Off."))
                .AssertReply(this.CheckForEndOfConversation())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_LaneAssistOffConfirmNo()
        {
            await this.GetTestFlow()
                .Send("turn lane assist off")
                .AssertReply(this.CheckReply("So, you want to change Lane Change Detection to Off. Is that correct? (1) Yes or (2) No"))
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
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Combined Set Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Increasing Rear Combined Set Temperature."))               
                .StartTestAsync();
        }
        
        [TestMethod]
        public async Task Test_DefogWindscreen()
        {
            await this.GetTestFlow()
                .Send("defog my windshield")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Window Defogger",
                    Value = "On",
                }))
                .AssertReply(this.CheckReply("Setting Rear Window Defogger to On."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AirOnFeet()
        {
            await this.GetTestFlow()
                .Send("put the air on my feet")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one? (1) Front Combined Air Delivery Mode Control(2) Rear Combined Air Delivery Mode Control"))
                .Send("front combined air delivery mode control")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Front Combined Air Delivery Mode Control",
                    Value = "Floor",
                }))
                .AssertReply(this.CheckReply("Setting Front Combined Air Delivery Mode Control to Floor."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ACOff()
        {
            await this.GetTestFlow()
                .Send("turn off the ac")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Front and Rear HVAC",
                    Value = "All Off",
                }))
                .AssertReply(this.CheckReply("Setting Front and Rear HVAC to All Off."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_FeelingCold()
        {
            await this.GetTestFlow()
                .Send("I'm feeling cold")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Increasing Temperature."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_FeelingColdInTheBack()
        {
            await this.GetTestFlow()
                .Send("it's feeling cold in the back")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Combined Set Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Increasing Rear Combined Set Temperature."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AdjustEqualizer()
        {
            await this.GetTestFlow()
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one? (1) Equalizer (Bass)(2) Equalizer (Midrange)(3) Equalizer (Treble)(4) Equalizer (Surround)"))
                .Send("Equalizer (Bass)")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))                
                 .Send("Decrease")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Equalizer (Bass)",
                    Value = "Decrease",
                }))
                .AssertReply(this.CheckReply("Decreasing Equalizer (Bass)."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithPartialMatches()
        {
            await this.GetTestFlow()
                .Send("change pedestrian detection")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one? (1) Front Pedestrian Safety Detection(2) Rear Pedestrian Safety Detection"))
                .Send("front")
                .AssertReply(this.CheckReply("Here are the possible values for Front Pedestrian Safety Detection. Which one? (1) Off(2) Alert(3) Alert and Brake(4) Alert, Brake, and Steer"))
                .Send("steer")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Front Pedestrian Safety Detection",
                    Value = "Alert, Brake, and Steer",
                }))
                .AssertReply(this.CheckReply("Setting Front Pedestrian Safety Detection to Alert, Brake, and Steer."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithSynonyms()
        {
            await this.GetTestFlow()
                .Send("change pedestrian detection")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one? (1) Front Pedestrian Safety Detection(2) Rear Pedestrian Safety Detection"))
                .Send("alerts for people in the back")
                .AssertReply(this.CheckReply("Here are the possible values for Rear Pedestrian Safety Detection. Which one? (1) Off(2) Alert(3) Alert and Brake(4) Alert, Brake, and Steer"))
                .Send("braking and alerts")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Pedestrian Safety Detection",
                    Value = "Alert and Brake",
                }))
                .AssertReply(this.CheckReply("Setting Rear Pedestrian Safety Detection to Alert and Brake."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithOrdinals()
        {
            await this.GetTestFlow()
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one? (1) Equalizer (Bass)(2) Equalizer (Midrange)(3) Equalizer (Treble)(4) Equalizer (Surround)"))
                .Send("first one")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))
                .Send("second one")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Equalizer (Bass)",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Increasing Equalizer (Bass)."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithIncorrectChoices()
        {
            await this.GetTestFlow()
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one? (1) Equalizer (Bass)(2) Equalizer (Midrange)(3) Equalizer (Treble)(4) Equalizer (Surround)"))
                .Send("blah blah")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one? (1) Equalizer (Bass)(2) Equalizer (Midrange)(3) Equalizer (Treble)(4) Equalizer (Surround)"))
                .Send("first one")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))
                .Send("blah blah")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))
                .Send("Decrease")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Equalizer (Bass)",
                    Value = "Decrease",
                }))
                .AssertReply(this.CheckReply("Decreasing Equalizer (Bass)."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_VerifyImagePathConfigurationUsedOnAdaptiveCard()
        {
            await this.GetTestFlow()
                .Send("adjust equalizer")
                .AssertReply(this.CheckImagePathOnAdaptiveCard())
                .Send("first one")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))
                .Send("blah blah")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))
                .Send("Decrease")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Equalizer (Bass)",
                    Value = "Decrease",
                }))
                .AssertReply(this.CheckReply("Decreasing Equalizer (Bass)."))
                .StartTestAsync();
        }

        private Action<IActivity> CheckForSettingEvent(SettingChange expectedChange)
        {
            return activity =>
            {
                var eventReceived = activity.AsEventActivity();
                Assert.IsNotNull(eventReceived,"Activity received is not an Event as expected");
                Assert.AreEqual<string>("AutomotiveSkill.SettingChange", eventReceived.Name);
                Assert.IsInstanceOfType(eventReceived.Value, typeof(SettingChange));
                Assert.AreEqual<SettingChange>(expectedChange, (SettingChange)eventReceived.Value);
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

        private Action<IActivity> CheckImagePathOnAdaptiveCard()
        {
            return activity =>
            {
                var messageReceived = activity.AsMessageActivity();
                var card = JsonConvert.DeserializeObject<ThumbnailCard>(messageReceived.Attachments[0].Content.ToString());
                Assert.IsTrue(card.Images[0].Url.StartsWith(ImageAssetLocation), $"Image URI was expected to be prefixed with {ImageAssetLocation} but was actually {card.Images[0].Url.ToString()}");
            };
        }
    }
}
