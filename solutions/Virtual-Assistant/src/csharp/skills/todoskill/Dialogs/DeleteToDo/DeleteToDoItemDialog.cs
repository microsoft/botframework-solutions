using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkill
{
    public class DeleteToDoItemDialog : ToDoSkillDialog
    {
        public DeleteToDoItemDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> accessor,
            IToDoService serviceManager)
            : base(nameof(DeleteToDoItemDialog), services, accessor, serviceManager)
        {
            var deleteToDoTask = new WaterfallStep[]
           {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                InitAllTasks,
                CollectToDoTaskIndex,
                CollectAskDeletionConfirmation,
                DeleteToDoTask,
           };

            var collectToDoTaskIndex = new WaterfallStep[]
            {
                AskToDoTaskIndex,
                AfterAskToDoTaskIndex,
            };

            var collectDeleteTaskConfirmation = new WaterfallStep[]
            {
                AskDeletionConfirmation,
                AfterAskDeletionConfirmation,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.DeleteToDoTask, deleteToDoTask));
            AddDialog(new WaterfallDialog(Action.CollectToDoTaskIndex, collectToDoTaskIndex));
            AddDialog(new WaterfallDialog(Action.CollectDeleteTaskConfirmation, collectDeleteTaskConfirmation));

            // Set starting dialog for component
            InitialDialogId = Action.DeleteToDoTask;
        }

        public async Task<DialogTurnResult> CollectAskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectDeleteTaskConfirmation);
        }

        public async Task<DialogTurnResult> DeleteToDoTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.DeleteTaskConfirmation)
                {
                    if (string.IsNullOrEmpty(state.OneNotePageId))
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOneNoteMessage));
                    }

                    var service = await _serviceManager.Init(state.MsGraphToken, state.OneNotePageId);
                    var page = await service.GetDefaultToDoPage();
                    string taskTopicToBeDeleted = null;
                    if (state.MarkOrDeleteAllTasksFlag)
                    {
                        await service.DeleteToDos(state.AllTasks, page.ContentUrl);
                        state.AllTasks = new List<ToDoItem>();
                        state.Tasks = new List<ToDoItem>();
                        state.ShowToDoPageIndex = 0;
                        state.TaskIndexes = new List<int>();
                    }
                    else
                    {
                        taskTopicToBeDeleted = state.AllTasks[state.TaskIndexes[0]].Topic;
                        var tasksToBeDeleted = new List<ToDoItem>();
                        state.TaskIndexes.ForEach(i => tasksToBeDeleted.Add(state.AllTasks[i]));
                        await service.DeleteToDos(tasksToBeDeleted, page.ContentUrl);
                        var todosAndPageIdTuple = await service.GetToDos();
                        state.OneNotePageId = todosAndPageIdTuple.Item2;
                        state.AllTasks = todosAndPageIdTuple.Item1;
                        var allTasksCount = state.AllTasks.Count;
                        var currentTaskIndex = state.ShowToDoPageIndex * state.PageSize;
                        while (currentTaskIndex >= allTasksCount && currentTaskIndex >= state.PageSize)
                        {
                            currentTaskIndex -= state.PageSize;
                            state.ShowToDoPageIndex--;
                        }

                        state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                    }

                    if (state.MarkOrDeleteAllTasksFlag)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(DeleteToDoResponses.AfterAllTasksDeleted));
                    }
                    else
                    {
                        if (state.Tasks.Count > 0)
                        {
                            var deletedToDoListAttachment = ToAdaptiveCardAttachmentForOtherFlows(
                                state.Tasks,
                                state.AllTasks.Count,
                                taskTopicToBeDeleted,
                                DeleteToDoResponses.AfterTaskDeleted,
                                ToDoSharedResponses.ShowToDoTasks);

                            var deletedToDoListReply = sc.Context.Activity.CreateReply();
                            deletedToDoListReply.Attachments.Add(deletedToDoListAttachment);
                            await sc.Context.SendActivityAsync(deletedToDoListReply);
                        }
                        else
                        {
                            var token1 = new StringDictionary() { { "taskContent", taskTopicToBeDeleted } };
                            var response1 = GenerateResponseWithTokens(DeleteToDoResponses.AfterTaskDeleted, token1);
                            var token2 = new StringDictionary() { { "taskCount", "0" } };
                            var response2 = GenerateResponseWithTokens(ToDoSharedResponses.ShowToDoTasks, token2);
                            var response = response1 + " " + response2.Remove(response2.Length - 1) + ".";
                            var botResponse = sc.Context.Activity.CreateReply(response);
                            botResponse.Speak = response;
                            await sc.Context.SendActivityAsync(botResponse);
                        }
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ActionEnded));
                }

                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    var prompt = sc.Context.Activity.CreateReply(DeleteToDoResponses.AskDeletionAllConfirmation);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
                else
                {
                    var toDoTask = state.Tasks[state.TaskIndexes[0]].Topic;
                    var token = new StringDictionary() { { "toDoTask", toDoTask } };
                    var response = GenerateResponseWithTokens(DeleteToDoResponses.AskDeletionConfirmation, token);
                    var prompt = sc.Context.Activity.CreateReply(response);
                    prompt.Speak = response;
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterAskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
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
                else if((promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false))
                {
                    state.DeleteTaskConfirmation = false;
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.CollectDeleteTaskConfirmation);
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }
    }
}
