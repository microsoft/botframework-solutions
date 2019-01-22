using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using static Microsoft.Recognizers.Text.Culture;

namespace ToDoSkill.Dialogs.Shared
{
    public class CollectTaskIndexPrompt : Prompt<List<int>>
    {
        public CollectTaskIndexPrompt(string dialogId, PromptValidator<List<int>> validator = null, string defaultLocale = null)
               : base(dialogId, validator)
        {
            DefaultLocale = defaultLocale;
        }

        public string DefaultLocale { get; set; }

        protected override async Task OnPromptAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, bool isRetry, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (isRetry && options.RetryPrompt != null)
            {
                await turnContext.SendActivityAsync(options.RetryPrompt, cancellationToken).ConfigureAwait(false);
            }
            else if (options.Prompt != null)
            {
                await turnContext.SendActivityAsync(options.Prompt, cancellationToken).ConfigureAwait(false);
            }
        }

        protected override async Task<PromptRecognizerResult<List<int>>> OnRecognizeAsync(ITurnContext turnContext, IDictionary<string, object> state, PromptOptions options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            var result = new PromptRecognizerResult<List<int>>();
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var message = turnContext.Activity.AsMessageActivity();
                var culture = turnContext.Activity.Locale ?? DefaultLocale ?? English;
                var taskIndexes = GetTaskIndexFromMessage();
                if (taskIndexes.Count > 0)
                {
                    result.Succeeded = true;
                    result.Value = taskIndexes;
                }
            }

            return await Task.FromResult(result);
        }

        private List<int> GetTaskIndexFromMessage(ITurnContext turnContext)
        {
            var state = await ToDoStateAccessor.GetAsync(turnContext);
            var matchedIndexes = Enumerable.Range(0, state.AllTasks.Count)
                .Where(i => state.AllTasks[i].Topic.Equals(state.TaskContentPattern, StringComparison.OrdinalIgnoreCase)
                || state.AllTasks[i].Topic.Equals(state.TaskContentML, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchedIndexes?.Count > 0)
            {
                state.TaskIndexes = matchedIndexes;
                return await sc.EndDialogAsync(true);
            }
            else
            {
                var userInput = sc.Context.Activity.Text;
                matchedIndexes = Enumerable.Range(0, state.AllTasks.Count)
                    .Where(i => state.AllTasks[i].Topic.Equals(userInput, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedIndexes?.Count > 0)
                {
                    state.TaskIndexes = matchedIndexes;
                    return await sc.EndDialogAsync(true);
                }
            }

            if (state.MarkOrDeleteAllTasksFlag)
            {
                return await sc.EndDialogAsync(true);
            }

            if (state.TaskIndexes.Count == 1
                && state.TaskIndexes[0] >= 0
                && state.TaskIndexes[0] < state.Tasks.Count)
            {
                state.TaskIndexes[0] = (state.PageSize * state.ShowTaskPageIndex) + state.TaskIndexes[0];
                return await sc.EndDialogAsync(true);
            }
            else
            {
                state.TaskContentPattern = null;
                state.TaskContentML = null;
                return await sc.ReplaceDialogAsync(Action.CollectTaskIndexForComplete);
            }
        }
    }
}
