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
using Microsoft.Bot.Solutions.Util;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;

namespace ToDoSkill
{
    public class ShowToDoItemDialog : ToDoSkillDialog
    {
        public ShowToDoItemDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ShowToDoItemDialog), services, toDoStateAccessor, userStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

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
            AddDialog(new WaterfallDialog(Action.ShowToDoTasks, showToDoTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.AddFirstTask, addFirstTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectToDoTaskContent, collectToDoTaskContent) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Action.ShowToDoTasks;
        }

        public async Task<DialogTurnResult> ShowToDoTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                state.LastListType = state.ListType;
                var service = await InitListTypeIds(sc);
                var topIntent = state.LuisResult?.TopIntent().intent;
                if (topIntent == ToDo.Intent.ShowToDo)
                {
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
                    if (topIntent == ToDo.Intent.ShowToDo)
                    {
                        toDoListAttachment = ToAdaptiveCardForShowToDos(
                            state.Tasks,
                            Math.Min(state.Tasks.Count, state.ReadSize),
                            state.AllTasks.Count);
                    }
                    else if (generalTopIntent == General.Intent.Next)
                    {
                        toDoListAttachment = ToAdaptiveCardForNextPage(
                            state.Tasks,
                            Math.Min(state.Tasks.Count, state.ReadSize));
                    }
                    else if (generalTopIntent == General.Intent.Previous)
                    {
                        toDoListAttachment = ToAdaptiveCardForPreviousPage(
                            state.Tasks,
                            Math.Min(state.Tasks.Count, state.ReadSize));
                    }
                    else if (generalTopIntent == General.Intent.ReadMore)
                    {
                        if (state.ReadTaskIndex == 0)
                        {
                            toDoListAttachment = ToAdaptiveCardForNextPage(
                            state.Tasks,
                            Math.Min(state.Tasks.Count, state.ReadSize));
                        }
                        else
                        {
                            var remainingTasksCount = state.Tasks.Count - (state.ReadTaskIndex * state.ReadSize);
                            toDoListAttachment = ToAdaptiveCardForReadMore(
                                state.Tasks,
                                state.ReadTaskIndex * state.ReadSize,
                                Math.Min(remainingTasksCount, state.ReadSize),
                                state.AllTasks.Count);
                        }
                    }

                    var toDoListReply = sc.Context.Activity.CreateReply();
                    toDoListReply.Attachments.Add(toDoListAttachment);
                    await sc.Context.SendActivityAsync(toDoListReply);
                    if (topIntent == ToDo.Intent.ShowToDo && allTasksCount > (state.ShowTaskPageIndex + 1) * state.PageSize)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ShowToDoResponses.ShowingMoreTasks));
                    }

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

        public async Task<DialogTurnResult> AddFirstTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.AddFirstTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskAddFirstTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = sc.Context.Activity.CreateReply(ShowToDoResponses.NoToDoTasksPrompt);
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskAddFirstTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var topIntent = state.GeneralLuisResult?.TopIntent().intent;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    state.TaskContent = null;
                    return await sc.NextAsync();
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ActionEnded));
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.AddFirstTask);
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
