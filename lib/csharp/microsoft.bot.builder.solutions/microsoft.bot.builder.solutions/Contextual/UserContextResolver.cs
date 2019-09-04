using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class UserContextResolver
    {
        public static int DialogIndex { get; set; } = 0;

        public IUserContext UserContext { get; set; }

        public void SetDialogIndex()
        {
            DialogIndex++;
        }

        public async Task ShowPreviousQuestion(ITurnContext turnContext)
        {
            var questionAccessor = turnContext.TurnState.Get<IStatePropertyAccessor<List<PreviousQuestion>>>();
            var questions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousQuestion>());
            var actions = questions.Select(x => x.Utterance).ToList();
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
