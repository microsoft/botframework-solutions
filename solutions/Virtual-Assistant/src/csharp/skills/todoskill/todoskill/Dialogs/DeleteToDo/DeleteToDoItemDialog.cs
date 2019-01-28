using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.Shared;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Models;
using ToDoSkill.ServiceClients;
using Action = ToDoSkill.Dialogs.Shared.Action;

namespace ToDoSkill.Dialogs.DeleteToDo
{
    public class DeleteToDoItemDialog : ToDoSkillDialog
    {
        public DeleteToDoItemDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(DeleteToDoItemDialog), services, toDoStateAccessor, userStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var deleteTask = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                CollectListTypeForDelete,
                InitAllTasks,
                DoDeleteTask,
            };

            var doDeleteTask = new WaterfallStep[]
            {
                CollectTaskIndexForDelete,
                CollectAskDeletionConfirmation,
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
            AddDialog(new WaterfallDialog(Action.DoDeleteTask, doDeleteTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.DeleteTask, deleteTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectListTypeForDelete, collectListTypeForDelete) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectTaskIndexForDelete, collectTaskIndexForDelete) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectDeleteTaskConfirmation, collectDeleteTaskConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.ContinueDeleteTask, continueDeleteTask) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Action.DeleteTask;
        }

        protected async Task<DialogTurnResult> DoDeleteTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.DoDeleteTask);
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
                var deletedTaskAttachment = new Attachment();
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

                    deletedTaskAttachment = ToAdaptiveCardForTaskDeletedFlow(
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

                        deletedTaskAttachment = ToAdaptiveCardForTaskDeletedFlow(
                        state.Tasks,
                        state.AllTasks.Count,
                        null,
                        state.ListType,
                        true);
                    }
                    else
                    {
                        deletedTaskAttachment = ToAdaptiveCardForDeletionRefusedFlow(
                        state.Tasks,
                        state.AllTasks.Count,
                        state.ListType);
                    }
                }

                var cardReply = sc.Context.Activity.CreateReply();
                cardReply.Attachments.Add(deletedTaskAttachment);
                await sc.Context.SendActivityAsync(cardReply);

                if (canDeleteAnotherTask)
                {
                    return await sc.NextAsync();
                }
                else
                {
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
                return await sc.BeginDialogAsync(Action.CollectListTypeForDelete);
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
                    var prompt = sc.Context.Activity.CreateReply(DeleteToDoResponses.ListTypePrompt);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
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
                    return await sc.ReplaceDialogAsync(Action.CollectListTypeForDelete);
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
                return await sc.BeginDialogAsync(Action.CollectTaskIndexForDelete);
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
                    var prompt = sc.Context.Activity.CreateReply(DeleteToDoResponses.AskTaskIndex);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
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
                    return await sc.ReplaceDialogAsync(Action.CollectTaskIndexForDelete);
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
                return await sc.BeginDialogAsync(Action.CollectDeleteTaskConfirmation);
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
                    var token = new StringDictionary() { { "listType", state.ListType } };
                    var response = GenerateResponseWithTokens(DeleteToDoResponses.AskDeletionAllConfirmation, token);
                    var prompt = sc.Context.Activity.CreateReply(response);
                    prompt.Speak = response;
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
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
                var luisResult = state.GeneralLuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    state.DeleteTaskConfirmation = true;
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    state.DeleteTaskConfirmation = false;
                    return await sc.EndDialogAsync(true);
                }
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
                return await sc.BeginDialogAsync(Action.ContinueDeleteTask);
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
                var prompt = sc.Context.Activity.CreateReply(DeleteToDoResponses.DeleteAnotherTaskPrompt);
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
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
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    // reset some fields here
                    state.TaskIndexes = new List<int>();
                    state.MarkOrDeleteAllTasksFlag = false;
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.TaskContent = null;

                    // replace current dialog to continue deleting more tasks
                    return await sc.ReplaceDialogAsync(Action.DoDeleteTask);
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ActionEnded));
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