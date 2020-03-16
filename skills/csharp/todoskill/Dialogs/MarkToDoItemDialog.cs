// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using ToDoSkill.Models;
using ToDoSkill.Responses.MarkToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MarkToDoItemDialog : ToDoSkillDialogBase
    {
        public MarkToDoItemDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials,
            IHttpContextAccessor httpContext)
            : base(nameof(MarkToDoItemDialog), settings, services, conversationState, userState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials, httpContext)
        {
            TelemetryClient = telemetryClient;

            var markTask = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                CollectListTypeForComplete,
                GetAuthToken,
                AfterGetAuthToken,
                InitAllTasks,
                DoMarkTask,
            };

            var doMarkTask = new WaterfallStep[]
            {
                CollectTaskIndexForComplete,
                GetAuthToken,
                AfterGetAuthToken,
                MarkTaskCompleted,
                ContinueMarkTask,
            };

            var collectListTypeForComplete = new WaterfallStep[]
            {
                AskListType,
                AfterAskListType,
            };

            var collectTaskIndexForComplete = new WaterfallStep[]
            {
                AskTaskIndex,
                AfterAskTaskIndex,
            };

            var continueMarkTask = new WaterfallStep[]
            {
                AskContinueMarkTask,
                AfterAskContinueMarkTask,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.DoMarkTask, doMarkTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.MarkTaskCompleted, markTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectListTypeForComplete, collectListTypeForComplete) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTaskIndexForComplete, collectTaskIndexForComplete) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ContinueMarkTask, continueMarkTask) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.MarkTaskCompleted;
        }

        protected async Task<DialogTurnResult> DoMarkTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoMarkTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> MarkTaskCompleted(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.LastListType = state.ListType;
                var service = await InitListTypeIds(sc);
                string taskTopicToBeMarked = null;
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    await service.MarkTasksCompletedAsync(state.ListType, state.AllTasks);
                    state.AllTasks.ForEach(task => task.IsCompleted = true);
                    state.ShowTaskPageIndex = 0;
                }
                else
                {
                    taskTopicToBeMarked = state.AllTasks[state.TaskIndexes[0]].Topic;
                    var tasksToBeMarked = new List<TaskItem>();
                    state.TaskIndexes.ForEach(i => tasksToBeMarked.Add(state.AllTasks[i]));
                    await service.MarkTasksCompletedAsync(state.ListType, tasksToBeMarked);
                    state.TaskIndexes.ForEach(i => state.AllTasks[i].IsCompleted = true);
                    state.ShowTaskPageIndex = state.TaskIndexes[0] / state.PageSize;
                }

                if (state.MarkOrDeleteAllTasksFlag)
                {
                    var markToDoCard = ToAdaptiveCardForTaskCompletedFlowByLG(
                        sc.Context,
                        state.Tasks,
                        state.AllTasks.Count,
                        taskTopicToBeMarked,
                        state.ListType,
                        state.MarkOrDeleteAllTasksFlag);
                    await sc.Context.SendActivityAsync(markToDoCard.Speak, speak: markToDoCard.Speak);
                }
                else
                {
                    var completedTaskIndex = state.AllTasks.FindIndex(t => t.IsCompleted == true);
                    var taskContent = state.AllTasks[completedTaskIndex].Topic;
                    var markToDoCard = ToAdaptiveCardForTaskCompletedFlowByLG(
                      sc.Context,
                      state.Tasks,
                      state.AllTasks.Count,
                      taskContent,
                      state.ListType,
                      state.MarkOrDeleteAllTasksFlag);
                    await sc.Context.SendActivityAsync(markToDoCard.Speak, speak: markToDoCard.Speak);

                    int uncompletedTaskCount = state.AllTasks.Where(t => t.IsCompleted == false).Count();
                    if (uncompletedTaskCount == 1)
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(MarkToDoResponses.AfterCompleteCardSummaryMessageForSingleTask, new { ListType = state.ListType });
                        await sc.Context.SendActivityAsync(activity);
                    }
                    else
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(MarkToDoResponses.AfterCompleteCardSummaryMessageForMultipleTasks, new { AllTasksCount = uncompletedTaskCount.ToString(), ListType = state.ListType });
                        await sc.Context.SendActivityAsync(activity);
                    }
                }

                return await sc.EndDialogAsync(true);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectListTypeForComplete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectListTypeForComplete);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskListType(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.ListType))
                {
                    var prompt = TemplateEngine.GenerateActivityForLocale(MarkToDoResponses.ListTypePromptForComplete);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = prompt });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskListType(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.ListType))
                {
                    return await sc.ReplaceDialogAsync(Actions.CollectListTypeForComplete);
                }
                else
                {
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectTaskIndexForComplete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectTaskIndexForComplete);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (!string.IsNullOrEmpty(state.TaskContentPattern)
                    || !string.IsNullOrEmpty(state.TaskContentML)
                    || state.MarkOrDeleteAllTasksFlag
                    || (state.TaskIndexes.Count == 1
                        && state.TaskIndexes[0] >= 0
                        && state.TaskIndexes[0] < state.Tasks.Count))
                {
                    return await sc.NextAsync();
                }
                else
                {
                    Activity prompt;
                    if (state.CollectIndexRetry)
                    {
                        prompt = TemplateEngine.GenerateActivityForLocale(MarkToDoResponses.AskTaskIndexRetryForComplete);
                    }
                    else
                    {
                        prompt = TemplateEngine.GenerateActivityForLocale(MarkToDoResponses.AskTaskIndexForComplete);
                    }

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = prompt });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.CollectIndexRetry = false;

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
                    state.CollectIndexRetry = true;
                    return await sc.ReplaceDialogAsync(Actions.CollectTaskIndexForComplete);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ContinueMarkTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ContinueMarkTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskContinueMarkTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateEngine.GenerateActivityForLocale(MarkToDoResponses.CompleteAnotherTaskPrompt);
                var retryPrompt = TemplateEngine.GenerateActivityForLocale(MarkToDoResponses.CompleteAnotherTaskConfirmFailed);
                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskContinueMarkTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    // reset some fields here
                    state.TaskIndexes = new List<int>();
                    state.MarkOrDeleteAllTasksFlag = false;
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.TaskContent = null;

                    // replace current dialog to continue marking more tasks
                    return await sc.ReplaceDialogAsync(Actions.DoMarkTask);
                }
                else
                {
                    await SendActionEnded(sc.Context);
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}