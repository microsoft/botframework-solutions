﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using ToDoSkill.Models;
using ToDoSkill.Responses.DeleteToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class DeleteToDoItemDialog : ToDoSkillDialogBase
    {
        public DeleteToDoItemDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials,
            IHttpContextAccessor httpContext)
            : base(nameof(DeleteToDoItemDialog), settings, services, conversationState, userState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials, httpContext)
        {
            TelemetryClient = telemetryClient;

            var deleteTask = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                CollectListTypeForDelete,
                GetAuthToken,
                AfterGetAuthToken,
                InitAllTasks,
                DoDeleteTask,
            };

            var doDeleteTask = new WaterfallStep[]
            {
                CollectTaskIndexForDelete,
                CollectAskDeletionConfirmation,
                GetAuthToken,
                AfterGetAuthToken,
                DeleteTask,
                ContinueDeleteTask,
            };

            var collectListTypeForDelete = new WaterfallStep[]
            {
                AskListType,
                AfterAskListType,
            };

            var collectTaskIndexForDelete = new WaterfallStep[]
            {
                AskTaskIndex,
                AfterAskTaskIndex,
            };

            var collectDeleteTaskConfirmation = new WaterfallStep[]
            {
                AskDeletionConfirmation,
                AfterAskDeletionConfirmation,
            };

            var continueDeleteTask = new WaterfallStep[]
            {
                AskContinueDeleteTask,
                AfterAskContinueDeleteTask,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.DoDeleteTask, doDeleteTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.DeleteTask, deleteTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectListTypeForDelete, collectListTypeForDelete) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTaskIndexForDelete, collectTaskIndexForDelete) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectDeleteTaskConfirmation, collectDeleteTaskConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ContinueDeleteTask, continueDeleteTask) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.DeleteTask;
        }

        protected async Task<DialogTurnResult> DoDeleteTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoDeleteTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> DeleteTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.LastListType = state.ListType;

                bool canDeleteAnotherTask = false;
                var cardReply = sc.Context.Activity.CreateReply();
                if (!state.MarkOrDeleteAllTasksFlag)
                {
                    var service = await InitListTypeIds(sc);
                    var taskTopicToBeDeleted = state.AllTasks[state.TaskIndexes[0]].Topic;
                    var tasksToBeDeleted = new List<TaskItem>();
                    state.TaskIndexes.ForEach(i => tasksToBeDeleted.Add(state.AllTasks[i]));
                    await service.DeleteTasksAsync(state.ListType, tasksToBeDeleted);
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                    var allTasksCount = state.AllTasks.Count;
                    var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
                    while (currentTaskIndex >= allTasksCount && currentTaskIndex >= state.PageSize)
                    {
                        currentTaskIndex -= state.PageSize;
                        state.ShowTaskPageIndex--;
                    }

                    state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));

                    cardReply = ToAdaptiveCardForTaskDeletedFlowByLG(
                        sc.Context,
                        state.Tasks,
                        state.AllTasks.Count,
                        taskTopicToBeDeleted,
                        state.ListType,
                        false);

                    canDeleteAnotherTask = state.AllTasks.Count > 0 ? true : false;
                }
                else
                {
                    if (state.DeleteTaskConfirmation)
                    {
                        var service = await InitListTypeIds(sc);
                        await service.DeleteTasksAsync(state.ListType, state.AllTasks);
                        state.AllTasks = new List<TaskItem>();
                        state.Tasks = new List<TaskItem>();
                        state.ShowTaskPageIndex = 0;
                        state.TaskIndexes = new List<int>();

                        cardReply = ToAdaptiveCardForTaskDeletedFlowByLG(
                            sc.Context,
                            state.Tasks,
                            state.AllTasks.Count,
                            string.Empty,
                            state.ListType,
                            true);
                    }
                    else
                    {
                        cardReply = ToAdaptiveCardForDeletionRefusedFlowByLG(
                            sc.Context,
                            state.Tasks,
                            state.AllTasks.Count,
                            state.ListType);
                    }
                }

                if (canDeleteAnotherTask)
                {
                    cardReply.InputHint = InputHints.IgnoringInput;
                    await sc.Context.SendActivityAsync(cardReply);
                    return await sc.NextAsync();
                }
                else
                {
                    cardReply.InputHint = InputHints.AcceptingInput;
                    await sc.Context.SendActivityAsync(cardReply);
                    return await sc.EndDialogAsync(true);
                }
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

        protected async Task<DialogTurnResult> CollectListTypeForDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectListTypeForDelete);
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
                    var prompt = TemplateEngine.GenerateActivityForLocale(DeleteToDoResponses.ListTypePromptForDelete);

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
                    return await sc.ReplaceDialogAsync(Actions.CollectListTypeForDelete);
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

        protected async Task<DialogTurnResult> CollectTaskIndexForDelete(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectTaskIndexForDelete);
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
                        prompt = TemplateEngine.GenerateActivityForLocale(DeleteToDoResponses.AskTaskIndexRetryForDelete);
                    }
                    else
                    {
                        prompt = TemplateEngine.GenerateActivityForLocale(DeleteToDoResponses.AskTaskIndexForDelete);
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
                    return await sc.ReplaceDialogAsync(Actions.CollectTaskIndexForDelete);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectAskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectDeleteTaskConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    var prompt = TemplateEngine.GenerateActivityForLocale(DeleteToDoResponses.AskDeletionAllConfirmation, new
                    {
                        ListType = state.ListType
                    });

                    var retryPrompt = TemplateEngine.GenerateActivityForLocale(DeleteToDoResponses.AskDeletionAllConfirmationFailed, new
                    {
                        ListType = state.ListType
                    });

                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
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

        protected async Task<DialogTurnResult> AfterAskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    var confirmResult = (bool)sc.Result;
                    if (confirmResult)
                    {
                        state.DeleteTaskConfirmation = true;
                    }
                    else
                    {
                        state.DeleteTaskConfirmation = false;
                    }
                }

                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ContinueDeleteTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.ContinueDeleteTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskContinueDeleteTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = TemplateEngine.GenerateActivityForLocale(DeleteToDoResponses.DeleteAnotherTaskPrompt);
                var retryPrompt = TemplateEngine.GenerateActivityForLocale(DeleteToDoResponses.DeleteAnotherTaskConfirmFailed);

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskContinueDeleteTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                    // replace current dialog to continue deleting more tasks
                    return await sc.ReplaceDialogAsync(Actions.DoDeleteTask);
                }
                else
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ActionEnded);
                    await sc.Context.SendActivityAsync(activity);

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