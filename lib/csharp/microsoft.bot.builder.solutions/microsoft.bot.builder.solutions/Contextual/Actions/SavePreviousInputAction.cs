using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class SavePreviousInputAction : ISkillContextualActions
    {
        public SavePreviousInputAction(
            ConversationState convState,
            UserState userState,
            UserContextManager userContextManager,
            string skillName,
            List<string> filter = null,
            int maxStoredQuestion = 7,
            ReplacementStrategy replacementStrategy = ReplacementStrategy.FIFO)
        {
            UserState = userState;
            ConversationState = convState;
            UserContextManager = userContextManager;
            SkillName = skillName;
            IntentFilter = filter;
            MaxStoredQuestion = maxStoredQuestion;
            Strategy = replacementStrategy;

            BeforeTurnAction = async turnContext =>
            {
                await InitPreviousInput(turnContext);
            };

            AfterTurnAction = async turnContext =>
            {
                await SavePreviousInputAsync(turnContext);
            };
        }

        private static int DialogIndex { get; set; } = -1;

        private UserState UserState { get; set; }

        private ConversationState ConversationState { get; set; }

        private UserContextManager UserContextManager { get; set; }

        private string SkillName { get; set; }

        private List<string> IntentFilter { get; set; }

        private int MaxStoredQuestion { get; set; }

        private ReplacementStrategy Strategy { get; set; }

        private async Task InitPreviousInput(ITurnContext turnContext)
        {
            var questionAccessor = UserState.CreateProperty<List<PreviousQuestion>>(string.Format("{0}Questions", SkillName));
            var questions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousQuestion>());
            UserContextManager.PreviousQuestions = questions;
        }

        private async Task SavePreviousInputAsync(ITurnContext turnContext)
        {
            if (IsTriggerIntent())
            {
                await SavePreviousQuestionAsync(turnContext);
            }

            await SavePreviousContact(turnContext);

            await UserState.SaveChangesAsync(turnContext);
        }

        private async Task<string> AbstractEmailPreviousContactAsync(ITurnContext turnContext)
        {
            try
            {
                var skillStateAccessor = ConversationState.CreateProperty<dynamic>(string.Format("{0}State", SkillName));
                var skillState = await skillStateAccessor.GetAsync(turnContext);
                var contacts = skillState.FindContactInfor.Contacts;
                return ((IEnumerable<dynamic>)contacts).Last().EmailAddress.Name;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> AbstractCalendarPreviousContactAsync(ITurnContext turnContext)
        {
            try
            {
                var skillStateAccessor = ConversationState.CreateProperty<dynamic>(string.Format("{0}State", SkillName));
                var skillState = await skillStateAccessor.GetAsync(turnContext);
                var contacts = skillState.MeetingInfor.ContactInfor.Contacts;
                return ((IEnumerable<dynamic>)contacts).Last().DisplayName;
            }
            catch
            {
                return null;
            }
        }

        // ToDo: this method only for email and calendar now.
        private async Task SavePreviousContact(ITurnContext turnContext)
        {
            string contact = null;
            switch (SkillName)
            {
                case "EmailSkill":
                    contact = await AbstractEmailPreviousContactAsync(turnContext);
                    break;
                case "CalendarSkill":
                    contact = await AbstractCalendarPreviousContactAsync(turnContext);
                    break;
            }

            if (contact != null)
            {
                if (UserContextManager.PreviousContacts.Contains(contact))
                {
                    UserContextManager.PreviousContacts.Remove(contact);
                }

                UserContextManager.PreviousContacts.Add(contact);
            }
        }

        private async Task<PreviousQuestion> AbstractPreviousQuestionItemsAsync(ITurnContext turnContext)
        {
            try
            {
                var skillStateAccessor = ConversationState.CreateProperty<dynamic>(string.Format("{0}State", SkillName));
                var skillState = await skillStateAccessor.GetAsync(turnContext);
                string utterance = skillState.LuisResult.Text;
                string intent = skillState.LuisResult.TopIntent().Item1.ToString();
                DateTimeOffset timeStamp = turnContext.Activity.Timestamp ?? new DateTimeOffset();

                return new PreviousQuestion()
                {
                    Utterance = utterance,
                    Intent = intent,
                    TimeStamp = timeStamp,
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task SavePreviousQuestionAsync(ITurnContext turnContext)
        {
            PreviousQuestion newQuestion = await AbstractPreviousQuestionItemsAsync(turnContext);
            if (newQuestion == null)
            {
                return;
            }

            // Don't save this intent.
            if (IntentFilter == null || !IntentFilter.Contains(newQuestion.Intent))
            {
                return;
            }

            var questionAccessor = UserState.CreateProperty<List<PreviousQuestion>>(string.Format("{0}Questions", SkillName));
            var previousQuestions = await questionAccessor.GetAsync(turnContext, () => new List<PreviousQuestion>());

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
