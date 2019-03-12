using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Dialogs.Shared.Resources;
using VirtualAssistant.Tests.TestHelpers;
using VirtualAssistant.Tests.Utterances;

namespace VirtualAssistant.Tests.SkillInvocationTests
{
    [TestClass]
    public class SkillInvocationTests : SkillTestBase
    {
        private Dictionary<string, SkillDefinition> skillDefinitions;

        [TestInitialize]
        public void InitSkills()
        {
            this.skillDefinitions = new Dictionary<string, SkillDefinition>();
            this.skillDefinitions.Add("calendarSkill", this.CreateSkillDefinition("calendarSkill", typeof(CalendarSkill.CalendarSkill)));
            this.skillDefinitions.Add("emailSkill", this.CreateSkillDefinition("emailSkill", typeof(EmailSkill.EmailSkill)));
            this.skillDefinitions.Add("toDoSkill", this.CreateSkillDefinition("todoSkill", typeof(ToDoSkill.ToDoSkill)));
            this.skillDefinitions.Add("pointOfInterestSkill", this.CreateSkillDefinition("pointOfInterestSkill", typeof(PointOfInterestSkill.PointOfInterestSkill)));

            foreach (SkillDefinition skill in this.skillDefinitions.Values)
            {
                // Add the SkillDialog to the available dialogs passing the initialized FakeSkill
                this.Dialogs.Add(new SkillDialog(skill, this.Services, this.ProactiveState, this.EndpointService, this.TelemetryClient, this.BackgroundTaskQueue));
            }
        }

        /// <summary>
        /// Test that we can invoke the Calendar Skill. If we are able to do this we will get an OAuth card.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task CalendarSkillInvocation()
        {
            this.SkillDialogOptions = new SkillDialogOptions();
            this.SkillDialogOptions.SkillDefinition = this.skillDefinitions["calendarSkill"];

            await this.GetTestFlow()
                .Send(CalendarUtterances.BookMeeting)
                .AssertReply((activity) => Assert.AreEqual(ActivityTypes.Trace, activity.Type))
                .AssertReply((activity) =>
                {
                    var messageActivity = activity.AsMessageActivity();
                    Assert.AreEqual(1, messageActivity.Attachments.Count);
                    Assert.AreEqual("application/vnd.microsoft.card.oauth", messageActivity.Attachments[0].ContentType);
                })
                .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the Email Skill. If we are able to do this we will get an OAuth card.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task EmailSkillInvocation()
        {
            this.SkillDialogOptions = new SkillDialogOptions();
            this.SkillDialogOptions.SkillDefinition = this.skillDefinitions["emailSkill"];

            await this.GetTestFlow()
                .Send(EmailUtterances.SendEmail)
                .AssertReply((activity) => Assert.AreEqual(ActivityTypes.Trace, activity.Type))
                .AssertReply((activity) =>
                {
                    var messageActivity = activity.AsMessageActivity();
                    Assert.AreEqual(1, messageActivity.Attachments.Count);
                    Assert.AreEqual("application/vnd.microsoft.card.oauth", messageActivity.Attachments[0].ContentType);
                })
                .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the ToDo Skill. If we are able to do this we will get an OAuth card.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task ToDoSkillInvocation()
        {
            this.SkillDialogOptions = new SkillDialogOptions();
            this.SkillDialogOptions.SkillDefinition = this.skillDefinitions["toDoSkill"];

            await this.GetTestFlow()
                .Send(ToDoUtterances.AddToDo)
                .AssertReply((activity) => Assert.AreEqual(ActivityTypes.Trace, activity.Type))
                .AssertReply((activity) =>
                {
                    var messageActivity = activity.AsMessageActivity();
                    Assert.AreEqual(1, messageActivity.Attachments.Count);
                    Assert.AreEqual("application/vnd.microsoft.card.oauth", messageActivity.Attachments[0].ContentType);
                })
                .StartTestAsync();
        }

        /// <summary>
        /// Test that we can invoke the PointOfInterest Skill and receive the location prompt.
        /// </summary>
        /// <returns>Task.</returns>
        [TestMethod]
        public async Task PointOfInterestSkillInvocation()
        {
            this.SkillDialogOptions = new SkillDialogOptions();
            this.SkillDialogOptions.SkillDefinition = this.skillDefinitions["pointOfInterestSkill"];

            await this.GetTestFlow()
            .Send(PointOfInterestUtterances.FindCoffeeShop)
            .AssertReply(CheckLocationPrompt())
            .StartTestAsync();
        }

        /// <summary>
        /// Asserts response is one of the available location prompts.
        /// </summary>
        /// <returns>IActivity.</returns>
        private Action<IActivity> CheckLocationPrompt()
        {
            return activity =>
            {
                var messageActivity = activity.AsMessageActivity();

                CollectionAssert.Contains(ParseReplies(POISharedResponses.PromptForCurrentLocation, new StringDictionary()), messageActivity.Text);
            };
        }
    }
}
