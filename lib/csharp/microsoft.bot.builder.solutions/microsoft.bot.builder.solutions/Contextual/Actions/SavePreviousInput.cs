using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class SavePreviousInput : ISkillContextualAction
    {
        public SavePreviousInput(
            ConversationState convState,
            UserState userState,
            UserContextManager userContextResolver,
            string skillName,
            List<string> filter = null,
            int maxStoredQuestion = 7,
            ReplacementStrategy replacementStrategy = ReplacementStrategy.FIFO)
        {
            UserState = userState;
            ConversationState = convState;
            UserContextResolver = userContextResolver;
            SkillName = skillName;
            IntentFilter = filter;
            MaxStoredQuestion = maxStoredQuestion;
            Strategy = replacementStrategy;

            BeforeTurnAction = async turnContext =>
            {
                await InitPreviousQuestion(turnContext);
            };

            AfterTurnAction = async turnContext =>
            {
                await SavePreviousQuestionAsync(turnContext);
            };
        }

        private static int DialogIndex { get; set; } = -1;

        private UserState UserState { get; set; }

        private ConversationState ConversationState { get; set; }

        private UserContextManager UserContextResolver { get; set; }

        private string SkillName { get; set; }

        private List<string> IntentFilter { get; set; }

        private int MaxStoredQuestion { get; set; }

        private ReplacementStrategy Strategy { get; set; }

        private async Task InitPreviousQuestion(ITurnContext turnContext)
        {
            var questionAccessor = UserState.CreateProperty<List<PreviousInput>>(string.Format("{0}Questions", SkillName));
            var questions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousInput>());
            UserContextResolver.PreviousQuestions = questions;
        }

        private async Task SavePreviousQuestionAsync(ITurnContext turnContext)
        {
            try
            {
                var newQuestion = await AbstractPreviousQuestionItemsAsync(turnContext);
                if (IsTriggerIntent())
                {
                    await ExecuteSavePreviousQuestionAsync(turnContext, newQuestion);
                }

                await UserState.SaveChangesAsync(turnContext);
            }
            catch
            {
            }
        }

        private async Task<PreviousInput> AbstractPreviousQuestionItemsAsync(ITurnContext turnContext)
        {
            var skillStateAccessor = ConversationState.CreateProperty<dynamic>(string.Format("{0}State", SkillName));
            var skillState = await skillStateAccessor.GetAsync(turnContext);
            string utterance = skillState.LuisResult.Text;
            string intent = skillState.LuisResult.TopIntent().Item1.ToString();
            DateTimeOffset timeStamp = turnContext.Activity.Timestamp ?? new DateTimeOffset();

            return new PreviousInput()
            {
                Utterance = utterance,
                Intent = intent,
                TimeStamp = timeStamp,
            };
        }

        private async Task ExecuteSavePreviousQuestionAsync(ITurnContext turnContext, PreviousInput newQuestion)
        {
            if (newQuestion == null)
            {
                return;
            }

            // Don't save this intent.
            if (!IntentFilter.Contains(newQuestion.Intent))
            {
                return;
            }

            var questionAccessor = UserState.CreateProperty<List<PreviousInput>>(string.Format("{0}Questions", SkillName));
            var previousQuestions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousInput>());

            // If already exists, refresh timestamp.
            var duplicateQuestion = previousQuestions.Where(x => x.Utterance == newQuestion.Utterance).ToList();
            if (duplicateQuestion != null && duplicateQuestion.Count > 0)
            {
                duplicateQuestion[0].TimeStamp = newQuestion.TimeStamp;
                return;
            }

            // Replace overdue item according to replacement strategy.
            if (previousQuestions.Count == MaxStoredQuestion)
            {
                var strategy = GetStrategy(Strategy);
                strategy.Replace(previousQuestions, newQuestion);
            }
            else
            {
                previousQuestions.Add(newQuestion);
            }
        }

        private IReplacementStrategy<PreviousInput> GetStrategy(ReplacementStrategy replacementStrategy)
        {
            switch (replacementStrategy)
            {
                case ReplacementStrategy.FIFO:
                    return new FIFOStrategy<PreviousInput>();
                case ReplacementStrategy.LRU:
                    return new LRUStrategy<PreviousInput>();
                case ReplacementStrategy.Random:
                    return new RandomStrategy<PreviousInput>();
                default:
                    return new FIFOStrategy<PreviousInput>();
            }
        }

        private bool IsTriggerIntent()
        {
            if (DialogIndex != UserContextManager.DialogIndex)
            {
                DialogIndex = UserContextManager.DialogIndex;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
