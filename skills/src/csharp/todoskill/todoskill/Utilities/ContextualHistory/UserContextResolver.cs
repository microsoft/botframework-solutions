using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using ToDoSkill.Utilities.ContextualHistory.Models;

namespace ToDoSkill.Utilities.ContextualHistory
{
    public class UserContextResolver
    {
        // ToDO: need to be thread safe.
        public int DialogIndex { get; set; } = 0;

        public IUserContext UserContext { get; set; }

        public void SetDialogIndex()
        {
            DialogIndex++;
        }

        public async Task SavePreviousQuestion(
            ITurnContext turnContext,
            string utterance,
            string intent,
            DateTimeOffset dateTimeOffset = default(DateTimeOffset))
        {
            await SkillContextualMiddleware.SavePreviousQuestion(turnContext, utterance, intent, dateTimeOffset);
        }

        public async Task ShowPreviousQuestion(ITurnContext turnContext)
        {
            var questionAccessor = turnContext.TurnState.Get<IStatePropertyAccessor<List<PreviousQuestion>>>();
            var questions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousQuestion>());

            var actions = questions.Select(x => x.Utterance).ToList<string>();
            var activity = MessageFactory.SuggestedActions(actions);
            await turnContext.SendActivityAsync(activity);
        }

        public async Task ClearPreviousQuestions(ITurnContext turnContext)
        {
            var questionAccessor = turnContext.TurnState.Get<IStatePropertyAccessor<List<PreviousQuestion>>>();
            await questionAccessor.DeleteAsync(turnContext);
        }
    }
}
