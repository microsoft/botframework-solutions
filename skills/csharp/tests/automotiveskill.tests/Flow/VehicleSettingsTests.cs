// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using AdaptiveCards;
using AutomotiveSkill.Models;
using AutomotiveSkill.Responses.Main;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AutomotiveSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class VehicleSettingsTests : AutomotiveSkillTestBase
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
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
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
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_IncreaseTemperatureByRelativeAmount()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
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
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_IncreaseTemperatureToAbsoluteAmount()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
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
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingTemperatureMissingValue()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("change the temperature")
                 .AssertReply(this.CheckReply("Here are the possible values for Temperature. Which one? (1) Decrease(2) Increase"))
                .Send("Increase")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_LaneAssistOffConfirmYes()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("turn lane assist off")
                .AssertReply(this.CheckReply("So, you want to change Lane Change Detection to Off. Is that correct? (1) Yes or (2) No"))
                .Send("yes")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Lane Change Detection",
                    Value = "Off",
                    IsConfirmed = true,
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_LaneAssistOffConfirmNo()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("turn lane assist off")
                .AssertReply(this.CheckReply("So, you want to change Lane Change Detection to Off. Is that correct? (1) Yes or (2) No"))
                .Send("no")
                .AssertReply(this.CheckReply("Ok, not making any changes."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_WarmUpBackOfCar()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("warm up the back of the car")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Combined Set Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_DefogWindscreen()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("defog my windshield")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Window Defogger",
                    Value = "On",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AirOnFeet()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("put the air on my feet")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one?\n\n   1. Front Combined Air Delivery Mode Control\n   2. Rear Combined Air Delivery Mode Control"))
                .Send("front combined air delivery mode control")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Front Combined Air Delivery Mode Control",
                    Value = "Floor",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_ACOff()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("turn off the ac")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Front and Rear HVAC",
                    Value = "All Off",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_FeelingCold()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("I'm feeling cold")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_FeelingColdInTheBack()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("it's feeling cold in the back")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Combined Set Temperature",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_AdjustEqualizer()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one?\n\n   1. Equalizer (Bass)\n   2. Equalizer (Midrange)\n   3. Equalizer (Treble)\n   4. Equalizer (Surround)"))
                .Send("Equalizer (Bass)")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))
                 .Send("Decrease")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Equalizer (Bass)",
                    Value = "Decrease",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithPartialMatches()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("change pedestrian detection")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one?\n\n   1. Front Pedestrian Safety Detection\n   2. Rear Pedestrian Safety Detection"))
                .Send("front")
                .AssertReply(this.CheckReply("Here are the possible values for Front Pedestrian Safety Detection. Which one?\n\n   1. Off\n   2. Alert\n   3. Alert and Brake\n   4. Alert, Brake, and Steer"))
                .Send("steer")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Front Pedestrian Safety Detection",
                    Value = "Alert, Brake, and Steer",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithSynonyms()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("change pedestrian detection")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one?\n\n   1. Front Pedestrian Safety Detection\n   2. Rear Pedestrian Safety Detection"))
                .Send("alerts for people in the back")
                .AssertReply(this.CheckReply("Here are the possible values for Rear Pedestrian Safety Detection. Which one?\n\n   1. Off\n   2. Alert\n   3. Alert and Brake\n   4. Alert, Brake, and Steer"))
                .Send("braking and alerts")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Rear Pedestrian Safety Detection",
                    Value = "Alert and Brake",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithOrdinals()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one?\n\n   1. Equalizer (Bass)\n   2. Equalizer (Midrange)\n   3. Equalizer (Treble)\n   4. Equalizer (Surround)"))
                .Send("first one")
                .AssertReply(this.CheckReply("Here are the possible values for Equalizer (Bass). Which one? (1) Decrease(2) Increase"))
                .Send("second one")
                .AssertReply(this.CheckForSettingEvent(new SettingChange()
                {
                    SettingName = "Equalizer (Bass)",
                    Value = "Increase",
                }))
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_SettingAndValueSelectionWithIncorrectChoices()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
                .Send("adjust equalizer")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one?\n\n   1. Equalizer (Bass)\n   2. Equalizer (Midrange)\n   3. Equalizer (Treble)\n   4. Equalizer (Surround)"))
                .Send("blah blah")
                .AssertReply(this.CheckReply("Here are the matching settings. Which one?\n\n   1. Equalizer (Bass)\n   2. Equalizer (Midrange)\n   3. Equalizer (Treble)\n   4. Equalizer (Surround)"))
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
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_VerifyImagePathConfigurationUsedOnAdaptiveCard()
        {
            await this.GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(AutomotiveSkillMainResponses.FirstPromptMessage))
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
                .AssertReply(this.CheckReply("Ok."))
                .StartTestAsync();
        }

        private Action<IActivity> CheckForSettingEvent(SettingChange expectedChange)
        {
            return activity =>
            {
                var eventReceived = activity.AsEventActivity();
                Assert.IsNotNull(eventReceived, "Activity received is not an Event as expected");
                Assert.IsTrue((eventReceived.Name ?? string.Empty).Contains("AutomotiveSkill."));
                Assert.IsInstanceOfType(eventReceived.Value, typeof(SettingChange));
                Assert.AreEqual<SettingChange>(expectedChange, (SettingChange)eventReceived.Value);
            };
        }

        private Action<IActivity> CheckForEndOfConversation()
        {
            return activity =>
            {
                Assert.AreEqual(activity.Type, ActivityTypes.EndOfConversation, "End of Conversation Activity not received.");
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
                var card = JsonConvert.DeserializeObject<AdaptiveCard>(messageReceived.Attachments[0].Content.ToString());
                Assert.IsInstanceOfType(card.Body[1], typeof(AdaptiveContainer));
                var container = (AdaptiveContainer)card.Body[1];
                Assert.IsInstanceOfType(container.Items[0], typeof(AdaptiveImage));
                var image = (AdaptiveImage)container.Items[0];
                Assert.IsTrue(image.Url.OriginalString.StartsWith(ImageAssetLocation), $"Image URI was expected to be prefixed with {ImageAssetLocation} but was actually {image.Url.OriginalString}");
            };
        }
    }
}
