using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkillTest.Flow.Fakes;
using CalendarSkillTest.Flow.Utterances;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Flow
{
    [TestClass]
    public class GeneralSkillFlowTests : CalendarBotTestBase
    {
        [TestInitialize]
        public void SetupLuisService()
        {
            var botServices = Services.BuildServiceProvider().GetService<BotServices>();
            botServices.CognitiveModelSets.Add("en", new CognitiveModelSet()
            {
                LuisServices = new Dictionary<string, ITelemetryRecognizer>()
                {
                    { "General", new MockLuisRecognizer() },
                    { "Calendar", new MockLuisRecognizer(new BaseTestUtterances()) }
                }
            });
        }

        /// <summary>
        /// Tests that single-turn conversations correctly call CompleteAsync on completion.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task Test_SingleTurnCompletion()
        {
            await this.GetTestFlow()
                .Send(GeneralTestUtterances.UnknownIntent)
                .AssertReplyOneOf(this.ConfusedResponse())
                .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.Handoff, activity.Type); })
                .StartTestAsync();
        }

        private string[] ConfusedResponse()
        {
            return this.ParseReplies(CalendarSharedResponses.DidntUnderstandMessage, new StringDictionary());
        }
    }
}
