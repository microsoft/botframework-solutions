using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using ToDoSkill.Dialogs.Shared;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkill.ServiceClients;
using Action = ToDoSkill.Dialogs.Shared.Action;

namespace ToDoSkill.Dialogs.ShowToDo
{
    public class ShowToDoItemDialog : ToDoSkillDialog
    {
        public ShowToDoItemDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ShowToDoItemDialog), services, responseManager, toDoStateAccessor, userStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var showTasks = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ClearContext,
                DoShowTasks,
            };

            var doShowTasks = new WaterfallStep[]
            {
                ShowTasks,
                FirstReadMoreTasks,
                SecondReadMoreTasks,
                CollectGoBackToStartConfirmation,
            };

            var firstReadMoreTasks = new WaterfallStep[]
            {
                CollectFirstReadMoreConfirmation,
                FirstReadMore,
            };

            var secondReadMoreTasks = new WaterfallStep[]
            {
                CollectSecondReadMoreConfirmation,
                SecondReadMore,
            };

            var collectFirstReadMoreConfirmation = new WaterfallStep[]
            {
                AskFirstReadMoreConfirmation,
                AfterAskFirstReadMoreConfirmation,
            };

            var collectSecondReadMoreConfirmation = new WaterfallStep[]
            {
                AskSecondReadMoreConfirmation,
                AfterAskSecondReadMoreConfirmation,
            };

            var collectGoBackToStartConfirmation = new WaterfallStep[]
            {
                AskGoBackToStartConfirmation,
                AfterAskGoBackToStartConfirmation,
            };

            var collectRepeatFirstPageConfirmation = new WaterfallStep[]
            {
                AskRepeatFirstPageConfirmation,
                AfterAskRepeatFirstPageConfirmation,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Action.ShowTasks, showTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.DoShowTasks, doShowTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.FirstReadMoreTasks, firstReadMoreTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.SecondReadMoreTasks, secondReadMoreTasks) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectFirstReadMoreConfirmation, collectFirstReadMoreConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectSecondReadMoreConfirmation, collectSecondReadMoreConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectGoBackToStartConfirmation, collectGoBackToStartConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Action.CollectRepeatFirstPageConfirmation, collectRepeatFirstPageConfirmation) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Action.ShowTasks;
        }

        public async Task<DialogTurnResult> DoShowTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.DoShowTasks);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ShowTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                state.LastListType = state.ListType;
                var service = await InitListTypeIds(sc);
                var topIntent = state.LuisResult?.TopIntent().intent;
                if (topIntent == ToDoLU.Intent.ShowToDo)
                {
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                }

                var allTasksCount = state.AllTasks.Count;
                var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
                state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;
                if (state.Tasks.Count <= 0)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ShowToDoResponses.NoTasksMessage, new StringDictionary() { { "listType", state.ListType } }));
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    var cardReply = sc.Context.Activity.CreateReply();

                    if (topIntent == ToDoLU.Intent.ShowToDo || state.GoBackToStart)
                    {
                        var toDoListAttachment = ToAdaptiveCardForShowToDos(
                            state.Tasks,
                            state.AllTasks.Count,
                            state.ReadSize,
                            state.ListType);

                        cardReply.Attachments.Add(toDoListAttachment);

                        if (state.Tasks.Count <= state.ReadSize)
                        {
                            cardReply.InputHint = InputHints.AcceptingInput;
                        }
                        else
                        {
                            cardReply.InputHint = InputHints.IgnoringInput;
                        }
                    }
                    else if (generalTopIntent == General.Intent.Next)
                    {
                        if (state.IsLastPage)
                        {
                            return await sc.ReplaceDialogAsync(Action.CollectGoBackToStartConfirmation);
                        }
                        else
                        {
                            var remainingTasksCount = state.Tasks.Count - (state.ReadTaskIndex * state.ReadSize);
                            var toDoListAttachment = ToAdaptiveCardForReadMore(
                                state.Tasks,
                                state.ReadTaskIndex * state.ReadSize,
                                Math.Min(remainingTasksCount, state.ReadSize),
                                state.AllTasks.Count,
                                state.ListType);

                            cardReply.Attachments.Add(toDoListAttachment);
                            cardReply.InputHint = InputHints.AcceptingInput;
                        }
                    }
                    else if (generalTopIntent == General.Intent.Previous)
                    {
                        if (state.IsFirstPage)
                        {
                            return await sc.ReplaceDialogAsync(Action.CollectRepeatFirstPageConfirmation);
                        }
                        else
                        {
                            var toDoListAttachment = ToAdaptiveCardForPreviousPage(
                                state.Tasks,
                                state.ReadSize,
                                state.AllTasks.Count,
                                state.ListType);

                            cardReply.Attachments.Add(toDoListAttachment);
                            cardReply.InputHint = InputHints.AcceptingInput;
                        }
                    }

                    await sc.Context.SendActivityAsync(cardReply);

                    if ((topIntent == ToDoLU.Intent.ShowToDo || state.GoBackToStart) && state.Tasks.Count > state.ReadSize)
                    {
                        state.GoBackToStart = false;
                        return await sc.NextAsync();
                    }
                    else
                    {
                        state.GoBackToStart = false;
                        return await sc.EndDialogAsync(true);
                    }
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

        public async Task<DialogTurnResult> FirstReadMoreTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.FirstReadMoreTasks);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectFirstReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectFirstReadMoreConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskFirstReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = ResponseManager.GetResponse(ShowToDoResponses.ReadMoreTasksPrompt);
                var retryPrompt = ResponseManager.GetResponse(ShowToDoResponses.ReadMoreTasksConfirmFailed);
                return await sc.PromptAsync(Action.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskFirstReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ShowToDoResponses.InstructionMessage));
                    return await sc.CancelAllDialogsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> FirstReadMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);

            state.ReadTaskIndex++;
            var remainingTasksCount = state.Tasks.Count - (state.ReadTaskIndex * state.ReadSize);
            var toDoListAttachment = ToAdaptiveCardForReadMore(
                    state.Tasks,
                    state.ReadTaskIndex * state.ReadSize,
                    Math.Min(remainingTasksCount, state.ReadSize),
                    state.AllTasks.Count,
                    state.ListType);

            var cardReply = sc.Context.Activity.CreateReply();
            cardReply.Attachments.Add(toDoListAttachment);

            if ((state.ShowTaskPageIndex + 1) * state.PageSize < state.AllTasks.Count)
            {
                cardReply.InputHint = InputHints.IgnoringInput;
                await sc.Context.SendActivityAsync(cardReply);
                return await sc.EndDialogAsync(true);
            }
            else
            {
                cardReply.InputHint = InputHints.AcceptingInput;
                await sc.Context.SendActivityAsync(cardReply);
                return await sc.CancelAllDialogsAsync();
            }
        }

        public async Task<DialogTurnResult> SecondReadMoreTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.SecondReadMoreTasks);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectSecondReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectSecondReadMoreConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskSecondReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var prompt = ResponseManager.GetResponse(ShowToDoResponses.ReadMoreTasksPrompt);
                var retryPrompt = ResponseManager.GetResponse(ShowToDoResponses.ReadMoreTasksConfirmFailed);
                return await sc.PromptAsync(Action.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskSecondReadMoreConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.ActionEnded));
                    return await sc.CancelAllDialogsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> SecondReadMore(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);

            if ((state.ReadTaskIndex + 1) * state.ReadSize < state.Tasks.Count)
            {
                state.ReadTaskIndex++;
            }
            else
            {
                // Go to next page if having more pages.
                state.ReadTaskIndex = 0;
                if ((state.ShowTaskPageIndex + 1) * state.PageSize < state.AllTasks.Count)
                {
                    state.ShowTaskPageIndex++;
                }
            }

            var allTasksCount = state.AllTasks.Count;
            var currentTaskIndex = state.ShowTaskPageIndex * state.PageSize;
            state.Tasks = state.AllTasks.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));

            var remainingTasksCount = state.Tasks.Count - (state.ReadTaskIndex * state.ReadSize);
            var toDoListAttachment = ToAdaptiveCardForReadMore(
                    state.Tasks,
                    state.ReadTaskIndex * state.ReadSize,
                    Math.Min(remainingTasksCount, state.ReadSize),
                    state.AllTasks.Count,
                    state.ListType);

            var cardReply = sc.Context.Activity.CreateReply();
            cardReply.Attachments.Add(toDoListAttachment);

            if ((state.ReadTaskIndex + 1) * state.ReadSize < state.Tasks.Count
                || (state.ShowTaskPageIndex + 1) * state.PageSize < state.AllTasks.Count)
            {
                cardReply.InputHint = InputHints.IgnoringInput;
                await sc.Context.SendActivityAsync(cardReply);
                return await sc.ReplaceDialogAsync(Action.SecondReadMoreTasks);
            }
            else
            {
                cardReply.InputHint = InputHints.AcceptingInput;
                await sc.Context.SendActivityAsync(cardReply);
                return await sc.EndDialogAsync(true);
            }
        }

        public async Task<DialogTurnResult> CollectGoBackToStartConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectGoBackToStartConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskGoBackToStartConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var token = new StringDictionary() { { "listType", state.ListType } };
                var prompt = ResponseManager.GetResponse(ShowToDoResponses.GoBackToStartPrompt, token);
                var retryPrompt = ResponseManager.GetResponse(ShowToDoResponses.GoBackToStartConfirmFailed);
                return await sc.PromptAsync(Action.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskGoBackToStartConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            var confirmResult = (bool)sc.Result;
            if (confirmResult)
            {
                state.ShowTaskPageIndex = 0;
                state.ReadTaskIndex = 0;
                state.GoBackToStart = true;
                return await sc.ReplaceDialogAsync(Action.DoShowTasks);
            }
            else
            {
                state.GoBackToStart = false;
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.ActionEnded));
                return await sc.EndDialogAsync(true);
            }
        }

        public async Task<DialogTurnResult> CollectRepeatFirstPageConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectGoBackToStartConfirmation);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AskRepeatFirstPageConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var token = new StringDictionary() { { "listType", state.ListType } };
                var prompt = ResponseManager.GetResponse(ShowToDoResponses.RepeatFirstPagePrompt, tokens: token);
                var retryPrompt = ResponseManager.GetResponse(ShowToDoResponses.RepeatFirstPageConfirmFailed);
                return await sc.PromptAsync(Action.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskRepeatFirstPageConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            var confirmResult = (bool)sc.Result;
            if (confirmResult)
            {
                state.ShowTaskPageIndex = 0;
                state.ReadTaskIndex = 0;
                state.GoBackToStart = true;
                return await sc.ReplaceDialogAsync(Action.DoShowTasks);
            }
            else
            {
                state.GoBackToStart = false;
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.ActionEnded));
                return await sc.EndDialogAsync(true);
            }
        }
    }
}