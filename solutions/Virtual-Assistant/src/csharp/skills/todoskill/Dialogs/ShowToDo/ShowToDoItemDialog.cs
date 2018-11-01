using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;

namespace ToDoSkill
{
    public class ShowToDoItemDialog : ToDoSkillDialog
    {
        public ShowToDoItemDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> accessor,
            ITaskService serviceManager)
            : base(nameof(ShowToDoItemDialog), services, accessor, serviceManager)
        {
            var showToDoTasks = new WaterfallStep[]
           {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                ShowToDoTasks,
                AddFirstTask,
           };

            var addFirstTask = new WaterfallStep[]
            {
                AskAddFirstTaskConfirmation,
                AfterAskAddFirstTaskConfirmation,
                CollectToDoTaskContent,
                AddToDoTask,
            };

            var collectToDoTaskContent = new WaterfallStep[]
            {
                AskToDoTaskContent,
                AfterAskToDoTaskContent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.ShowToDoTasks, showToDoTasks));
            AddDialog(new WaterfallDialog(Action.AddFirstTask, addFirstTask));
            AddDialog(new WaterfallDialog(Action.CollectToDoTaskContent, collectToDoTaskContent));

            // Set starting dialog for component
            InitialDialogId = Action.ShowToDoTasks;
        }

        public async Task<DialogTurnResult> ShowToDoTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (!state.ListTypeIds.ContainsKey(state.ListType))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOneNoteMessage));
                }

                var topIntent = state.LuisResult?.TopIntent().intent;
                if (topIntent == ToDo.Intent.ShowToDo || topIntent == ToDo.Intent.None)
                {
                    var service = await _serviceManager.InitAsync(state.MsGraphToken, state.ListTypeIds);
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                }

                var allTasksCount = state.AllTasks.Count;
                var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
                state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;
                if (state.Tasks.Count <= 0)
                {
                    return await sc.NextAsync();
                }
                else
                {
                    Attachment toDoListAttachment = null;
                    if (topIntent == ToDo.Intent.ShowToDo || topIntent == ToDo.Intent.None)
                    {
                        toDoListAttachment = ToAdaptiveCardAttachmentForShowToDos(
                            state.Tasks,
                            state.AllTasks.Count,
                            ToDoSharedResponses.ShowToDoTasks,
                            ShowToDoResponses.ReadToDoTasks);
                    }
                    else if (generalTopIntent == General.Intent.Next)
                    {
                        toDoListAttachment = ToAdaptiveCardAttachmentForShowToDos(
                            state.Tasks,
                            state.AllTasks.Count,
                            ShowToDoResponses.ShowNextToDoTasks,
                            null);
                    }
                    else if (generalTopIntent == General.Intent.Previous)
                    {
                        toDoListAttachment = ToAdaptiveCardAttachmentForShowToDos(
                            state.Tasks,
                            state.AllTasks.Count,
                            ShowToDoResponses.ShowPreviousToDoTasks,
                            null);
                    }

                    var toDoListReply = sc.Context.Activity.CreateReply();
                    toDoListReply.Attachments.Add(toDoListAttachment);
                    await sc.Context.SendActivityAsync(toDoListReply);
                    if ((topIntent == ToDo.Intent.ShowToDo || topIntent == ToDo.Intent.None)
                        && allTasksCount > (state.ShowTaskPageIndex + 1) * state.PageSize)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ShowToDoResponses.ShowingMoreTasks));
                    }

                    return await sc.EndDialogAsync(true);
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AddFirstTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.AddFirstTask);
        }

        public async Task<DialogTurnResult> AskAddFirstTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var prompt = sc.Context.Activity.CreateReply(ShowToDoResponses.NoToDoTasksPrompt);
            return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
        }

        public async Task<DialogTurnResult> AfterAskAddFirstTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var topIntent = state.GeneralLuisResult?.TopIntent().intent;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    state.TaskContent = null;
                    return await sc.NextAsync();
                }
                else if ((promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ActionEnded));
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.AddFirstTask);
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
