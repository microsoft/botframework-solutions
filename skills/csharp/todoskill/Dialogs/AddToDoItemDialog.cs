﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
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
using ToDoSkill.Responses.AddToDo;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class AddToDoItemDialog : ToDoSkillDialogBase
    {
        public AddToDoItemDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials,
            IHttpContextAccessor httpContext)
            : base(nameof(AddToDoItemDialog), settings, services, conversationState, userState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials, httpContext)
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
                GetAuthToken,
                AfterGetAuthToken,
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
                GetAuthToken,
                AfterGetAuthToken,
                AskAddDupTaskConfirmation,
                AfterAskAddDupTaskConfirmation,
            };

            var continueAddTask = new WaterfallStep[]
            {
                AskContinueAddTask,
                AfterAskContinueAddTask,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.DoAddTask, doAddTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.AddTask, addTask) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTaskContent, collectTaskContent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectSwitchListTypeConfirmation, collectSwitchListTypeConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectAddDupTaskConfirmation, collectAddDupTaskConfirmation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ContinueAddTask, continueAddTask) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.AddTask;
        }

        protected async Task<DialogTurnResult> DoAddTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.DoAddTask);
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

                    var toDoListCard = ToAdaptiveCardForTaskAddedFlowByLG(
                        sc.Context,
                        state.Tasks,
                        state.TaskContent,
                        state.AllTasks.Count,
                        state.ListType);
                    await sc.Context.SendActivityAsync(toDoListCard);

                    return await sc.NextAsync();
                }
                else
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ActionEnded);
                    await sc.Context.SendActivityAsync(activity);
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
                return await sc.BeginDialogAsync(Actions.CollectTaskContent);
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
                    var prompt = TemplateEngine.GenerateActivityForLocale(AddToDoResponses.AskTaskContentText);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = prompt });
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
                        return await sc.ReplaceDialogAsync(Actions.CollectTaskContent);
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
                    return await sc.BeginDialogAsync(Actions.CollectSwitchListTypeConfirmation);
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

                var prompt = TemplateEngine.GenerateActivityForLocale(AddToDoResponses.SwitchListType, new
                {
                    ListType = state.ListType
                });

                var retryPrompt = TemplateEngine.GenerateActivityForLocale(AddToDoResponses.SwitchListTypeConfirmFailed, new
                {
                    ListType = state.ListType
                });

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
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
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
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
                return await sc.BeginDialogAsync(Actions.CollectAddDupTaskConfirmation);
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
                    var prompt = TemplateEngine.GenerateActivityForLocale(AddToDoResponses.AskAddDupTaskPrompt, new
                    {
                        TaskContent = state.TaskContent
                    });

                    var retryPrompt = TemplateEngine.GenerateActivityForLocale(AddToDoResponses.AskAddDupTaskConfirmFailed, new
                    {
                        TaskContent = state.TaskContent
                    });

                    return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
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
                    var confirmResult = (bool)sc.Result;
                    if (confirmResult)
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
                return await sc.BeginDialogAsync(Actions.ContinueAddTask);
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

                var prompt = TemplateEngine.GenerateActivityForLocale(AddToDoResponses.AddMoreTask, new
                {
                    ListType = state.ListType
                });

                var retryPrompt = TemplateEngine.GenerateActivityForLocale(AddToDoResponses.AddMoreTaskConfirmFailed, new
                {
                    ListType = state.ListType
                });

                return await sc.PromptAsync(Actions.ConfirmPrompt, new PromptOptions() { Prompt = prompt, RetryPrompt = retryPrompt });
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

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    // reset some fields here
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.ShopContent = null;
                    state.TaskContent = null;
                    state.FoodOfGrocery = null;
                    state.HasShopVerb = false;

                    // replace current dialog to continue add more tasks
                    return await sc.ReplaceDialogAsync(Actions.DoAddTask);
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
                state.TaskContent = state.TaskContentML ?? state.TaskContentPattern;
                if (string.IsNullOrEmpty(state.ListType))
                {
                    state.ListType = ToDoStrings.ToDo;
                }
            }
        }
    }
}