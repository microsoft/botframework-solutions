using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using ToDoSkill.Dialogs.AddToDo.Resources;
using ToDoSkill.Dialogs.Shared;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.ServiceClients;
using Action = ToDoSkill.Dialogs.Shared.Action;

namespace ToDoSkill.Dialogs.AddToDo
{
    public class AddToDoItemDialog : ToDoSkillDialog
    {
        public AddToDoItemDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(AddToDoItemDialog), services, toDoStateAccessor, userStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var addTask = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                DoAddTask,
            };

            var doAddTask = new WaterfallStep[]
            {
                CollectTaskContent,
                CollectSwitchListTypeConfirmation,
                CollectAddDupTaskConfirmation,
                AddTask,
                ContinueAddTask,
            };

            var collectTaskContent = new WaterfallStep[]
            {
                AskTaskContent,
                AfterAskTaskContent,
            };

            var collectSwitchListTypeConfirmation = new WaterfallStep[]
            {
                AskSwitchListTypeConfirmation,
                AfterAskSwitchListTypeConfirmation,
            };

            var collectAddDupTaskConfirmation = new WaterfallStep[]
            {
                AskAddDupTaskConfirmation,
                AfterAskAddDupTaskConfirmation,
            };

            var continueAddTask = new WaterfallStep[]
            {
                AskContinueAddTask,
                AfterAskContinueAddTask,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.DoAddTask, doAddTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.AddTask, addTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectTaskContent, collectTaskContent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectSwitchListTypeConfirmation, collectSwitchListTypeConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectAddDupTaskConfirmation, collectAddDupTaskConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.ContinueAddTask, continueAddTask) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Action.AddTask;
        }

        protected async Task<DialogTurnResult> DoAddTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.DoAddTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AddTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (state.AddDupTask)
                {
                    state.ListType = state.ListType ?? ToDoStrings.ToDo;
                    state.LastListType = state.ListType;
                    var service = await InitListTypeIds(sc);
                    var currentAllTasks = await service.GetTasksAsync(state.ListType);
                    var duplicatedTaskIndex = currentAllTasks.FindIndex(t => t.Topic.Equals(state.TaskContent, StringComparison.InvariantCultureIgnoreCase));

                    await service.AddTaskAsync(state.ListType, state.TaskContent);
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                    state.ShowTaskPageIndex = 0;
                    var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                    state.Tasks = state.AllTasks.GetRange(0, rangeCount);
                    var toDoListAttachment = ToAdaptiveCardForTaskAddedFlow(
                        state.Tasks,
                        state.TaskContent,
                        state.AllTasks.Count,
                        state.ListType);

                    var cardReply = sc.Context.Activity.CreateReply();
                    cardReply.Attachments.Add(toDoListAttachment);
                    await sc.Context.SendActivityAsync(cardReply);

                    return await sc.NextAsync();
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ActionEnded));
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

        protected async Task<DialogTurnResult> CollectTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectTaskContent);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.ToDoStateAccessor.GetAsync(sc.Context);
                if (!string.IsNullOrEmpty(state.TaskContentPattern)
                    || !string.IsNullOrEmpty(state.TaskContentML)
                    || !string.IsNullOrEmpty(state.ShopContent))
                {
                    return await sc.NextAsync();
                }
                else
                {
                    var prompt = sc.Context.Activity.CreateReply(AddToDoResponses.AskTaskContentText);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.TaskContentPattern)
                    && string.IsNullOrEmpty(state.TaskContentML)
                    && string.IsNullOrEmpty(state.ShopContent))
                {
                    if (sc.Result != null)
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var toDoContent);
                        state.TaskContent = toDoContent != null ? toDoContent.ToString() : sc.Context.Activity.Text;
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(Action.CollectTaskContent);
                    }
                }
                else
                {
                    this.ExtractListTypeAndTaskContent(state);
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectSwitchListTypeConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (state.SwitchListType)
                {
                    state.SwitchListType = false;
                    return await sc.BeginDialogAsync(Action.CollectSwitchListTypeConfirmation);
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

        protected async Task<DialogTurnResult> AskSwitchListTypeConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var token = new StringDictionary() { { "listType", state.ListType } };
                var response = GenerateResponseWithTokens(AddToDoResponses.SwitchListType, token);
                var prompt = sc.Context.Activity.CreateReply(response);
                prompt.Speak = response;
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskSwitchListTypeConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    state.ListType = state.LastListType;
                    state.LastListType = null;
                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectAddDupTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectAddDupTaskConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskAddDupTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                state.LastListType = state.ListType;
                var service = await InitListTypeIds(sc);
                var currentAllTasks = await service.GetTasksAsync(state.ListType);
                state.AddDupTask = false;
                var duplicatedTaskIndex = currentAllTasks.FindIndex(t => t.Topic.Equals(state.TaskContent, StringComparison.InvariantCultureIgnoreCase));
                if (duplicatedTaskIndex < 0)
                {
                    state.AddDupTask = true;
                    return await sc.NextAsync();
                }
                else
                {
                    var token = new StringDictionary() { { "taskContent", state.TaskContent } };
                    var prompt = sc.Context.Activity.CreateReply(AddToDoResponses.AskAddDupTaskPrompt, tokens: token);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskAddDupTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                if (!state.AddDupTask)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                    if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                    {
                        state.AddDupTask = true;
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

        protected async Task<DialogTurnResult> ContinueAddTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.ContinueAddTask);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskContinueAddTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var prompt = sc.Context.Activity.CreateReply(AddToDoResponses.AddMoreTask, tokens: new StringDictionary() { { "listType", state.ListType } });
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskContinueAddTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.ShopContent = null;
                    state.TaskContent = null;
                    state.FoodOfGrocery = null;
                    state.HasShopVerb = false;

                    // replace current dialog to continue add more tasks
                    return await sc.ReplaceDialogAsync(Action.DoAddTask);
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

        private void ExtractListTypeAndTaskContent(ToDoSkillState state)
        {
            if (state.HasShopVerb && !string.IsNullOrEmpty(state.FoodOfGrocery))
            {
                if (state.ListType != ToDoStrings.Grocery)
                {
                    state.LastListType = state.ListType;
                    state.ListType = ToDoStrings.Grocery;
                    state.SwitchListType = true;
                }
            }
            else if (state.HasShopVerb && !string.IsNullOrEmpty(state.ShopContent))
            {
                if (state.ListType != ToDoStrings.Shopping)
                {
                    state.LastListType = state.ListType;
                    state.ListType = ToDoStrings.Shopping;
                    state.SwitchListType = true;
                }
            }

            if (state.ListType == ToDoStrings.Grocery || state.ListType == ToDoStrings.Shopping)
            {
                state.TaskContent = string.IsNullOrEmpty(state.ShopContent) ? state.TaskContentML ?? state.TaskContentPattern : state.ShopContent;
            }
            else
            {
                state.ListType = ToDoStrings.ToDo;
                state.TaskContent = state.TaskContentML ?? state.TaskContentPattern;
            }
        }
    }
}