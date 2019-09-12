using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Contextual.Models.Strategy;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class CachePreviousTriggerIntentAction : SkillContextualActionBase
    {
        public CachePreviousTriggerIntentAction(
            ConversationstateAbstractor conversationstateAbstractor,
            UserState userState,
            UserContextManager userContextManager,
            List<string> filter = null,
            int maxStoredQuestion = 7,
            ReplacementStrategy replacementStrategy = ReplacementStrategy.FIFO)
        {
            ConversationstateAbstractor = conversationstateAbstractor;
            UserState = userState;
            UserContextManager = userContextManager;
            IntentFilter = filter;
            MaxStoredQuestion = maxStoredQuestion;
            Strategy = replacementStrategy;

            BeforeTurnAction = async turnContext =>
            {
                await InitPreviousTriggerIntent(turnContext);
            };

            AfterTurnAction = async turnContext =>
            {
                await CachePreviousTriggerIntentAsync(turnContext);
            };
        }

        private static int DialogIndex { get; set; } = -1;

        private ConversationstateAbstractor ConversationstateAbstractor { get; set; }

        private UserState UserState { get; set; }

        private UserContextManager UserContextManager { get; set; }

        private List<string> IntentFilter { get; set; }

        private int MaxStoredQuestion { get; set; }

        private ReplacementStrategy Strategy { get; set; }

        private async Task InitPreviousTriggerIntent(ITurnContext turnContext)
        {
            var triggerIntentsAccessor = UserState.CreateProperty<List<PreviousTriggerIntent>>("PreviousTriggerIntent");
            var triggerIntents = await triggerIntentsAccessor.GetAsync(turnContext, () => new List<PreviousTriggerIntent>());
            UserContextManager.PreviousTriggerIntents = triggerIntents;
        }

        private async Task CachePreviousTriggerIntentAsync(ITurnContext turnContext)
        {
            if (IsTriggerIntent())
            {
                await ExcuteCachePreviousTriggerIntentAsync(turnContext);
            }

            await UserState.SaveChangesAsync(turnContext);
        }

        private async Task<PreviousTriggerIntent> AbstractPreviousTriggerIntentAsync(ITurnContext turnContext)
        {
            try
            {
                var properties = await ConversationstateAbstractor.AbstractTargetPropertiesAsync(turnContext);
                return new PreviousTriggerIntent()
                {
                    Utterance = properties[0],
                    Intent = properties[1],
                    TimeStamp = turnContext.Activity.Timestamp ?? new DateTimeOffset(),
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task ExcuteCachePreviousTriggerIntentAsync(ITurnContext turnContext)
        {
            PreviousTriggerIntent newTriggerIntent = await AbstractPreviousTriggerIntentAsync(turnContext);
            if (newTriggerIntent == null)
            {
                return;
            }

            // Don't save this intent.
            if (IntentFilter == null || !IntentFilter.Contains(newTriggerIntent.Intent))
            {
                return;
            }

            var triggerIntentsAccessor = UserState.CreateProperty<List<PreviousTriggerIntent>>("PreviousTriggerIntent");
            var triggerIntents = await triggerIntentsAccessor.GetAsync(turnContext, () => new List<PreviousTriggerIntent>());

            // If already exists, refresh timestamp.
            var duplicateTriggerIntent = triggerIntents.Where(x => x.Utterance == newTriggerIntent.Utterance).ToList();
            if (duplicateTriggerIntent != null && duplicateTriggerIntent.Count > 0)
            {
                duplicateTriggerIntent[0].TimeStamp = newTriggerIntent.TimeStamp;
                return;
            }

            // Replace overdue item according to replacement strategy.
            if (triggerIntents.Count == MaxStoredQuestion)
            {
                var strategy = GetStrategy(Strategy);
                strategy.Replace(triggerIntents, newTriggerIntent);
            }
            else
            {
                triggerIntents.Add(newTriggerIntent);
            }
        }

        private IReplacementStrategy<PreviousTriggerIntent> GetStrategy(ReplacementStrategy replacementStrategy)
        {
            switch (replacementStrategy)
            {
                case ReplacementStrategy.FIFO:
                    return new FIFOStrategy<PreviousTriggerIntent>();
                case ReplacementStrategy.LRU:
                    return new LRUStrategy<PreviousTriggerIntent>();
                case ReplacementStrategy.Random:
                    return new RandomStrategy<PreviousTriggerIntent>();
                default:
                    return new FIFOStrategy<PreviousTriggerIntent>();
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
