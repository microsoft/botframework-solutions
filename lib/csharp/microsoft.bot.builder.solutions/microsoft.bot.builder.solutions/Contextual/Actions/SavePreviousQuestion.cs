using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class SavePreviousQuestion : SkillContextualActionsBase
    {
        public SavePreviousQuestion(
            ConversationState convState,
            UserState userState,
            UserContextResolver userContextResolver,
            string skillName,
            List<string> filter = null,
            int maxStoredQuestion = 7,
            ReplacementStrategy replacementStrategy = ReplacementStrategy.FIFO)
        {
            SkillStateAccessor = convState.CreateProperty<dynamic>(string.Format("{0}State", skillName));
            QuestionAccessor = userState.CreateProperty<List<PreviousQuestion>>(string.Format("{0}Questions", skillName));
            UserContextResolver = userContextResolver;
            IntentFilter = filter;
            MaxStoredQuestion = maxStoredQuestion;
            Strategy = replacementStrategy;

            BeforeTurnAction = turnContext =>
            {
                InitPreviousQuestion(turnContext);
            };

            AfterTurnAction = async turnContext =>
            {
                await SavePreviousQuestionAsync(turnContext);
            };

            uu = userState;
        }

        private UserState uu;

        private static int DialogIndex { get; set; } = -1;

        private IStatePropertyAccessor<dynamic> SkillStateAccessor { get; set; }

        private IStatePropertyAccessor<List<PreviousQuestion>> QuestionAccessor { get; set; }

        private UserContextResolver UserContextResolver { get; set; }

        private List<string> IntentFilter { get; set; }

        private int MaxStoredQuestion { get; set; }

        private ReplacementStrategy Strategy { get; set; }

        private void InitPreviousQuestion(ITurnContext turnContext)
        {
            turnContext.TurnState.Add(QuestionAccessor);
        }

        private async Task SavePreviousQuestionAsync(ITurnContext turnContext)
        {
            try
            {
                var userState = await SkillStateAccessor.GetAsync(turnContext);
                string utterance = userState.LuisResult.Text;
                string intent = userState.LuisResult.TopIntent().Item1.ToString();
                DateTimeOffset timeStamp = turnContext.Activity.Timestamp ?? new DateTimeOffset();

                if (IsTriggerIntent())
                {
                    await ExecuteSavePreviousQuestionAsync(turnContext, utterance, intent, timeStamp);
                }
            }
            catch
            {
            }
        }

        private async Task ExecuteSavePreviousQuestionAsync(
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


            var t = uu.CreateProperty<List<PreviousQuestion>>(string.Format("{0}Questions", "ToDoSkill"));
            var tt = t.GetAsync(turnContext);
            var ttt = 6;
        }

        private IReplacementStrategy<PreviousQuestion> GetStrategy(ReplacementStrategy replacementStrategy)
        {
            switch (replacementStrategy)
            {
                case ReplacementStrategy.FIFO:
                    return new FIFOStrategy<PreviousQuestion>();
                case ReplacementStrategy.LRU:
                    return new LRUStrategy<PreviousQuestion>();
                case ReplacementStrategy.Random:
                    return new RandomStrategy<PreviousQuestion>();
                default:
                    return new FIFOStrategy<PreviousQuestion>();
            }
        }

        private bool IsTriggerIntent()
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
