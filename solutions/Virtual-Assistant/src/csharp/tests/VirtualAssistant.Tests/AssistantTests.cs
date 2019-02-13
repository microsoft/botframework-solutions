using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using VirtualAssistant.Dialogs.Escalate.Resources;
using VirtualAssistant.Dialogs.Main.Resources;
using VirtualAssistant.Dialogs.Onboarding.Resources;
using VirtualAssistant.Tests.TestHelpers;
using VirtualAssistant.Tests.Utterances;

namespace VirtualAssistant.Tests
{
    /// <summary>
    /// Virtual Assistant Unit Tests.
    /// </summary>
    [TestClass]
    public class AssistantTests : AssistantTestBase
    {
        [TestMethod]
        public async Task IntroCard()
        {
            var startConversationEvent = new Activity
            {
                Type = ActivityTypes.Event,
                Name = "startConversation",
                Locale = "en-us",
            };

            await this.GetTestFlow()
               .Send(startConversationEvent)
               .AssertReply(ValidateEventReceived(startConversationEvent.Name))
               .AssertReply(ValidateIntroCard())
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Greeting()
        {
            await this.GetTestFlow()
               .Send(GeneralUtterances.Greeting)
               .AssertReply(MainStrings.GREETING)
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Escalate()
        {
            await this.GetTestFlow()
               .Send(GeneralUtterances.Escalate)
               .AssertReply(EscalateStrings.PHONE_INFO)
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Help()
        {
            await this.GetTestFlow()
               .Send(GeneralUtterances.Help)
               .AssertReply(ValidateHeroCardResponse())
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Confused()
        {
            await this.GetTestFlow()
               .Send("Blah Blah")
               .AssertReply(MainStrings.CONFUSED)
               .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the signout logic
        /// Test Adapter doesn't support SignOutUser so an exception indicates we've gone into the logic.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidOperationException), "OAuthPrompt.SignOutUser(): not supported by the current adapter")]
        public async Task Logout()
        {
            await this.GetTestFlow()
               .Send(GeneralUtterances.Logout)
               .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the Email Skill through VA. If we are able to do this we will at this time get an Exception saying that the
        /// Test Adapter doesn't support GetUserToken which signals the skill has been invoked and the GetAuth step has started
        /// signalling the Skill has completed.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidOperationException), "OAuthPrompt.GetUserToken(): not supported by the current adapter")]
        public async Task CalendarSkillInvocation()
        {
            await this.GetTestFlow()
               .Send(CalendarUtterances.BookMeeting)
               .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the Email Skill through VA. If we are able to do this we will at this time get an Exception saying that the
        /// Test Adapter doesn't support GetUserToken which signals the skill has been invoked and the GetAuth step has started
        /// signalling the Skill has completed.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidOperationException), "OAuthPrompt.GetUserToken(): not supported by the current adapter")]
        public async Task EmailSkillInvocation()
        {
            await this.GetTestFlow()
               .Send(EmailUtterances.SendEmail)
               .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the ToDo Skill. If we are able to do this we will at this time get an Exception saying that the
        /// Test Adapter doesn't support GetUserToken which signals the skill has been invoked and the GetAuth step has started
        /// signalling the Skill has completed.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        [ExpectedExceptionAndMessage(typeof(InvalidOperationException), "OAuthPrompt.GetUserToken(): not supported by the current adapter")]
        public async Task ToDoSkillInvocation()
        {
            await this.GetTestFlow()
            .Send(ToDoUtterances.AddToDo)
            .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the PointOfInterest Skill, if we are able to do this we will get a trace activity saying that the
        /// Azure Maps key isn't available (through configuration) which is expected and proves we can invoke the skill.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task PointOfInterestSkillInvocation()
        {
            await this.GetTestFlow()
            .Send(PointOfInterestUtterances.FindCoffeeShop)
            .AssertReply(ValidateSkillForwardingTrace("pointOfInterestSkill"))
            .AssertReply(this.ValidateAzureMapsKeyPrompt())
            .AssertReply(this.CheckForSkillInvocationError())

            // .AssertReply(this.CheckForEndOfConversationEvent())
            .StartTestAsync();
        }

        /// <summary>
        /// Test that location events are processed correctly.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task LocationEventProcessed()
        {
            var locationEvent = new Activity
            {
                Type = ActivityTypes.Event,
                Name = "IPA.Location",
                Value = "47.5977502,-122.1861507"
            };

            await this.GetTestFlow()
               .Send(locationEvent)
               .AssertReply(ValidateEventReceived(locationEvent.Name))
               .StartTestAsync();
        }

        /// <summary>
        /// Test that reset user events are processed correctly.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task ResetUserEventProcessed()
        {
            var locationEvent = new Activity
            {
                Type = ActivityTypes.Event,
                Name = "IPA.ResetUser",
            };

            await this.GetTestFlow()
               .Send(locationEvent)
               .AssertReply(ValidateEventReceived(locationEvent.Name))
               .AssertReply(ValidateTraceMessage("Reset User Event received, clearing down State and Tokens."))
               .StartTestAsync();
        }

        /// <summary>
        /// Test that timezone events are processed correctly.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task TimeZoneEventProcessed()
        {
            var locationEvent = new Activity
            {
                Type = ActivityTypes.Event,
                Name = "IPA.Timezone",
                Value = "Pacific Standard Time"
            };

            await this.GetTestFlow()
               .Send(locationEvent)
               .AssertReply(ValidateEventReceived(locationEvent.Name))
               .StartTestAsync();
        }

        private Action<IActivity> ValidateIntroCard()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual("application/vnd.microsoft.card.adaptive", messageActivity.Attachments[0].ContentType);

                var cardJson = JsonConvert.SerializeObject(messageActivity.Attachments[0].Content);
                var parseResult = AdaptiveCard.FromJson(cardJson);
                Assert.IsTrue(parseResult.Warnings.Count == 0, "Intro Card has Adaptive Card parse warnings");
            };
        }

        private Action<IActivity> ValidateHeroCardResponse()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual("application/vnd.microsoft.card.hero", messageActivity.Attachments[0].ContentType);
                Assert.IsFalse(string.IsNullOrEmpty(messageActivity.Speak));
            };
        }

        private Action<IActivity> ValidateGreeting()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                Assert.AreEqual(messageActivity.Text, MainStrings.GREETING);
            };
        }

        private Action<IActivity> ValidateTraceMessage(string message)
        {
            return activity =>
            {
                var traceActivity = activity as Activity;
                Assert.IsNotNull(traceActivity);

                Assert.IsTrue(traceActivity.Text.Equals(message));
            };
        }

        private Action<IActivity> ValidateSkillForwardingTrace(string skillName)
        {
            return activity =>
            {
                var traceActivity = activity as Activity;
                Assert.IsNotNull(traceActivity);

                Assert.IsTrue(traceActivity.Text.Contains($"-->Forwarding your utterance to the {skillName} skill."));
            };
        }

        private Action<IActivity> ValidateEventReceived(string eventName)
        {
            return activity =>
            {
                var traceActivity = activity as Activity;
                Assert.IsNotNull(traceActivity);

                Assert.IsTrue(traceActivity.Text.Contains($"Received event: {eventName}"));
            };
        }

        private Action<IActivity> ValidateAzureMapsKeyPrompt()
        {
            return activity =>
            {
                var traceActivity = activity as Activity;
                Assert.IsNotNull(traceActivity);

                Assert.IsTrue(traceActivity.Text.Contains("Skill Error: Could not get the Azure Maps key. Please make sure your settings are correctly configured."));
            };
        }

        private Action<IActivity> CheckForSkillInvocationError()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();
                var response = ResponseManager.GetResponseTemplate(CommonResponses.ErrorMessage_SkillError);
                Assert.AreEqual(messageActivity.Text, response.Reply.Text);
            };
        }

        private Action<IActivity> CheckForEndOfConversationEvent()
        {
            return activity =>
            {
                var traceActivity = activity as Activity;
                Assert.IsNotNull(traceActivity);

                Assert.AreEqual(traceActivity.Type, ActivityTypes.Trace);
                Assert.AreEqual(traceActivity.Text, "<--Ending the skill conversation");
            };
        }
    }
}
