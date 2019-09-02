using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using ToDoSkill.Utilities.ContextualHistory.Models;
using ToDoSkill.Utilities.ContextualHistory.Models.Strategy;

namespace ToDoSkill.Utilities.ContextualHistory
{
    public class SkillContextualMiddleware : IMiddleware
    {
        public SkillContextualMiddleware(
            ConversationState convState,
            UserState userState,
            UserContextResolver userContextResolver,
            string skillName,
            List<string> filter = null,
            int maxStoredQuestion = 7,
            ReplacementStrategy replacementStrategy = ReplacementStrategy.FIFO)
        {
            SkillStateAccessor = convState.CreateProperty<dynamic>(string.Format("{0}State", skillName));
            QuestionAccessor = userState.CreateProperty<List<PreviousQuestion>>(string.Format("{0}Questions1", skillName));
            UserContextResolver = userContextResolver;
            IntentFilter = filter;
            MaxStoredQuestion = maxStoredQuestion;
            Strategy = replacementStrategy;
        }

        private static List<string> IntentFilter { get; set; }

        private static int MaxStoredQuestion { get; set; }

        private static ReplacementStrategy Strategy { get; set; }

        private static int DialogIndex { get; set; } = -1;

        private IStatePropertyAccessor<dynamic> SkillStateAccessor { get; set; }

        private IStatePropertyAccessor<List<PreviousQuestion>> QuestionAccessor { get; set; }

        private UserContextResolver UserContextResolver { get; set; }

        public static async Task SavePreviousQuestion(
           ITurnContext turnContext,
           string utterance,
           string intent,
           DateTimeOffset timeStamp = default(DateTimeOffset))
        {
            if (utterance == null || intent == null || timeStamp == null)
            {
                return;
            }

            var question = new PreviousQuestion();
            question.Utterance = utterance;
            question.Intent = intent;
            question.TimeStamp = timeStamp;

            // Don't save this intent.
            if (!IntentFilter.Contains(intent))
            {
                return;
            }

            var questionAccessor = turnContext.TurnState.Get<IStatePropertyAccessor<List<PreviousQuestion>>>();
            var previousQuestions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousQuestion>());

            // If already exists, refresh timestamp.
            var duplicateQuestion = previousQuestions.Where(x => x.Utterance == utterance).ToList();
            if (duplicateQuestion != null && duplicateQuestion.Count > 0)
            {
                duplicateQuestion[0].TimeStamp = timeStamp;
                return;
            }

            // Replace overdue item according to replacement strategy.
            if (previousQuestions.Count == MaxStoredQuestion)
            {
                var strategy = GetStrategy(Strategy);
                strategy.Replace(previousQuestions, question);
            }
            else
            {
                previousQuestions.Add(question);
            }
        }

        public static IReplacementStrategy GetStrategy(ReplacementStrategy replacementStrategy)
        {
            switch (replacementStrategy)
            {
                case ReplacementStrategy.FIFO:
                    return new FIFOStrategy();
                case ReplacementStrategy.LRU:
                    return new LRUStrategy();
                case ReplacementStrategy.Random:
                    return new RandomStrategy();
                default:
                    return new FIFOStrategy();
            }
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            turnContext.TurnState.Add(QuestionAccessor);

            await next(cancellationToken).ConfigureAwait(false);

            // Save question in this turn.
            try
            {
                var userState = await SkillStateAccessor.GetAsync(turnContext);
                string utterance = userState.LuisResult.Text;
                string intent = userState.LuisResult.TopIntent().Item1.ToString();
                DateTimeOffset timeStamp = turnContext.Activity.Timestamp ?? new DateTimeOffset();

                // Only save trigger intent.
                if (IsTriggerIntent())
                {
                    await SavePreviousQuestion(turnContext, utterance, intent, timeStamp);
                }
            }
            catch
            {
            }
        }

        public bool IsTriggerIntent()
        {
            if (DialogIndex != UserContextResolver.DialogIndex)
            {
                DialogIndex = UserContextResolver.DialogIndex;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
