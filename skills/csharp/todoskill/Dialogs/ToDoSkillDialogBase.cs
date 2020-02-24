// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text;
using SkillServiceLibrary.Utilities;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Models;
using ToDoSkill.Responses.Shared;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class ToDoSkillDialogBase : ComponentDialog
    {
        private const string Synonym = "Synonym";
        private IHttpContextAccessor _httpContext;
        private BotSettings _settings;

        public ToDoSkillDialogBase(
            string dialogId,
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials,
            IHttpContextAccessor httpContext)
            : base(dialogId)
        {
            _httpContext = httpContext;
            _settings = settings;
            Services = services;

            // Initialize state accessor
            ToDoStateAccessor = conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            UserStateAccessor = userState.CreateProperty<ToDoSkillUserState>(nameof(ToDoSkillUserState));

            TemplateEngine = localeTemplateEngineManager;

            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            AddDialog(new MultiProviderAuthDialog(settings.OAuthConnections));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.ConfirmPrompt, null, Culture.English) { Style = ListStyle.SuggestedAction });
        }

        protected LocaleTemplateEngineManager TemplateEngine { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<ToDoSkillState> ToDoStateAccessor { get; set; }

        protected IStatePropertyAccessor<ToDoSkillUserState> UserStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DigestToDoLuisResult(dc);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DigestToDoLuisResult(dc);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var resultString = result?.ToString();
            if (!string.IsNullOrWhiteSpace(resultString) && resultString.Equals(CommonUtil.DialogTurnResultCancelAllDialogs, StringComparison.InvariantCultureIgnoreCase) && outerDc.Parent.ActiveDialog.Id != nameof(MainDialog))
            {
                return outerDc.CancelAllDialogsAsync();
            }
            else
            {
                return base.EndComponentAsync(outerDc, result, cancellationToken);
            }
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var retryPrompt = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.NoAuth);
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = retryPrompt });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {

                    if (sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token))
                    {
                        sc.Context.TurnState[StateProperties.APIToken] = providerTokenResponse.TokenResponse.Token;
                    }
                    else
                    {
                        sc.Context.TurnState.Add(StateProperties.APIToken, providerTokenResponse.TokenResponse.Token);
                    }
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ClearContext(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                var luisResult = sc.Context.TurnState.Get<ToDoLuis>(StateProperties.ToDoLuisResultKey);
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var topIntent = luisResult.TopIntent().intent;
                var generalTopIntent = generalLuisResult.TopIntent().intent;

                if (topIntent == ToDoLuis.Intent.ShowToDo)
                {
                    state.ShowTaskPageIndex = 0;
                    state.Tasks = new List<TaskItem>();
                    state.AllTasks = new List<TaskItem>();
                    state.ListType = null;
                    state.GoBackToStart = false;
                    await DigestToDoLuisResult(sc);
                }
                else if (topIntent == ToDoLuis.Intent.ShowNextPage || generalTopIntent == General.Intent.ShowNext)
                {
                    state.IsLastPage = false;
                    if ((state.ShowTaskPageIndex + 1) * state.PageSize < state.AllTasks.Count)
                    {
                        state.ShowTaskPageIndex++;
                    }
                    else
                    {
                        state.IsLastPage = true;
                    }
                }
                else if (topIntent == ToDoLuis.Intent.ShowPreviousPage || generalTopIntent == General.Intent.ShowPrevious)
                {
                    state.IsFirstPage = false;
                    if (state.ShowTaskPageIndex > 0)
                    {
                        state.ShowTaskPageIndex--;
                    }
                    else
                    {
                        state.IsFirstPage = true;
                    }
                }
                else if (topIntent == ToDoLuis.Intent.AddToDo)
                {
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.TaskContent = null;
                    state.FoodOfGrocery = null;
                    state.ShopContent = null;
                    state.HasShopVerb = false;
                    state.ListType = null;
                    await DigestToDoLuisResult(sc);
                }
                else if (topIntent == ToDoLuis.Intent.MarkToDo || topIntent == ToDoLuis.Intent.DeleteToDo)
                {
                    state.TaskIndexes = new List<int>();
                    state.MarkOrDeleteAllTasksFlag = false;
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.TaskContent = null;
                    state.CollectIndexRetry = false;
                    await DigestToDoLuisResult(sc);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> InitAllTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);

                // LastListType is used to switch between list types in DeleteToDoItemDialog and MarkToDoItemDialog.
                if (!state.ListTypeIds.ContainsKey(state.ListType)
                    || state.ListType != state.LastListType)
                {
                    var service = await InitListTypeIds(sc);
                    state.AllTasks = await service.GetTasksAsync(state.ListType);
                    state.ShowTaskPageIndex = 0;
                    var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                    state.Tasks = state.AllTasks.GetRange(0, rangeCount);
                }

                if (state.AllTasks.Count <= 0)
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.NoTasksInList);
                    await sc.Context.SendActivityAsync(activity);
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.NextAsync();
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

        // Validators
        protected Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        // Helpers
        protected async Task DigestToDoLuisResult(DialogContext dc)
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(dc.Context);
                var luisResult = dc.Context.TurnState.Get<ToDoLuis>(StateProperties.ToDoLuisResultKey);
                var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var entities = luisResult.Entities;
                var generalEntities = generalLuisResult.Entities;
                if (entities.ContainsAll != null)
                {
                    state.MarkOrDeleteAllTasksFlag = true;
                }

                if (entities.ordinal != null || (generalEntities != null && generalEntities.number != null))
                {
                    var indexOfOrdinal = entities.ordinal == null ? 0 : (int)entities.ordinal[0];
                    var indexOfNumber = generalEntities?.number == null ? 0 : (int)generalEntities.number[0];
                    var index = 0;
                    if (indexOfOrdinal > 0 && indexOfOrdinal <= state.PageSize)
                    {
                        index = indexOfOrdinal;
                    }
                    else if (indexOfNumber > 0 && indexOfNumber <= state.PageSize)
                    {
                        index = indexOfNumber;
                    }

                    if (index > 0 && index <= state.PageSize)
                    {
                        if (state.TaskIndexes.Count > 0)
                        {
                            state.TaskIndexes[0] = index - 1;
                        }
                        else
                        {
                            state.TaskIndexes.Add(index - 1);
                        }
                    }
                }

                if (entities.ListType != null)
                {
                    var topListType = entities.ListType[0];

                    var toDoStringProperties = typeof(ToDoStrings).GetProperties();
                    foreach (PropertyInfo toDoStringProperty in toDoStringProperties)
                    {
                        var listTypeSynonymKey = toDoStringProperty.Name;
                        if (listTypeSynonymKey.Contains(Synonym))
                        {
                            string listTypeSynonymValue = toDoStringProperty.GetValue(null).ToString();
                            if (listTypeSynonymValue.Contains(topListType, StringComparison.InvariantCultureIgnoreCase))
                            {
                                string listTypeKey = listTypeSynonymKey.Substring(0, listTypeSynonymKey.Length - Synonym.Length);
                                state.ListType = toDoStringProperties.Where(x => x.Name == listTypeKey).First().GetValue(null).ToString();
                            }
                        }
                    }
                }

                if (entities.FoodOfGrocery != null)
                {
                    state.FoodOfGrocery = entities.FoodOfGrocery[0][0];
                }

                if (entities.ShopVerb != null && (entities.TaskContent != null || entities.FoodOfGrocery != null))
                {
                    state.HasShopVerb = true;
                }

                if (entities.TaskContent != null)
                {
                    state.ShopContent = entities.TaskContent[0];
                }

                if (entities.TaskContent != null)
                {
                    state.TaskContentML = entities.TaskContent[0];
                }
            }
            catch
            {
                // ToDo
            }
        }

        protected Activity ToAdaptiveCardForShowToDosByLG(
           ITurnContext turnContext,
           List<TaskItem> todos,
           int allTasksCount,
           string listType)
        {
            bool useFile = Channel.GetChannelId(turnContext) == Channels.Msteams;

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ShowToDo, new
            {
                AllTasksCount = allTasksCount,
                ListType = listType,
                Title = string.Format(ToDoStrings.CardTitle, listType),
                TotalNumber = allTasksCount > 1 ? string.Format(ToDoStrings.CardMultiNumber, allTasksCount.ToString()) : string.Format(ToDoStrings.CardOneNumber, allTasksCount.ToString()),
                ToDos = todos,
                UseFile = useFile,
                CheckIconUrl = useFile ? GetImageUri(IconImageSource.CheckIconFile) : IconImageSource.CheckIconSource,
                UnCheckIconUrl = useFile ? GetImageUri(IconImageSource.UncheckIconFile) : IconImageSource.UncheckIconSource
            });
            activity.Speak += todos.ToSpeechString(CommonStrings.And, li => li.Topic);
            return activity;
        }

        protected Activity ToAdaptiveCardForReadMoreByLG(
            ITurnContext turnContext,
            List<TaskItem> todos,
            int allTasksCount,
            string listType)
        {
            bool useFile = Channel.GetChannelId(turnContext) == Channels.Msteams;

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ReadMore, new
            {
                Title = string.Format(ToDoStrings.CardTitle, listType),
                TotalNumber = allTasksCount > 1 ? string.Format(ToDoStrings.CardMultiNumber, allTasksCount.ToString()) : string.Format(ToDoStrings.CardOneNumber, allTasksCount.ToString()),
                ToDos = todos,
                UseFile = useFile,
                CheckIconUrl = useFile ? GetImageUri(IconImageSource.CheckIconFile) : IconImageSource.CheckIconSource,
                UnCheckIconUrl = useFile ? GetImageUri(IconImageSource.UncheckIconFile) : IconImageSource.UncheckIconSource
            });
            activity.Speak += todos.ToSpeechString(CommonStrings.And, li => li.Topic);
            return activity;
        }

        protected Activity ToAdaptiveCardForPreviousPageByLG(
            ITurnContext turnContext,
            List<TaskItem> todos,
            int allTasksCount,
            bool isFirstPage,
            string listType)
        {
            bool useFile = Channel.GetChannelId(turnContext) == Channels.Msteams;

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.PreviousPage, new
            {
                Title = string.Format(ToDoStrings.CardTitle, listType),
                TotalNumber = allTasksCount > 1 ? string.Format(ToDoStrings.CardMultiNumber, allTasksCount.ToString()) : string.Format(ToDoStrings.CardOneNumber, allTasksCount.ToString()),
                ToDos = todos,
                UseFile = useFile,
                CheckIconUrl = useFile ? GetImageUri(IconImageSource.CheckIconFile) : IconImageSource.CheckIconSource,
                UnCheckIconUrl = useFile ? GetImageUri(IconImageSource.UncheckIconFile) : IconImageSource.UncheckIconSource
            });
            activity.Speak += todos.ToSpeechString(CommonStrings.And, li => li.Topic);
            return activity;
        }

        protected Activity ToAdaptiveCardForTaskAddedFlowByLG(
            ITurnContext turnContext,
            List<TaskItem> todos,
            string taskContent,
            int allTasksCount,
            string listType)
        {
            bool useFile = Channel.GetChannelId(turnContext) == Channels.Msteams;

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.AfterTaskAdded, new
            {
                TaskContent = taskContent,
                ListType = listType,
                Title = string.Format(ToDoStrings.CardTitle, listType),
                TotalNumber = allTasksCount > 1 ? string.Format(ToDoStrings.CardMultiNumber, allTasksCount.ToString()) : string.Format(ToDoStrings.CardOneNumber, allTasksCount.ToString()),
                ToDos = todos,
                UseFile = useFile,
                CheckIconUrl = useFile ? GetImageUri(IconImageSource.CheckIconFile) : IconImageSource.CheckIconSource,
                UnCheckIconUrl = useFile ? GetImageUri(IconImageSource.UncheckIconFile) : IconImageSource.UncheckIconSource
            });
            return activity;
        }

        protected Activity ToAdaptiveCardForTaskCompletedFlowByLG(
            ITurnContext turnContext,
            List<TaskItem> todos,
            int allTasksCount,
            string taskContent,
            string listType,
            bool isCompleteAll)
        {
            bool useFile = Channel.GetChannelId(turnContext) == Channels.Msteams;

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.TaskCompleted, new
            {
                AllTasksCount = allTasksCount,
                ListType = listType,
                IsCompleteAll = isCompleteAll,
                TaskContent = taskContent,
                Title = string.Format(ToDoStrings.CardTitle, listType),
                TotalNumber = allTasksCount > 1 ? string.Format(ToDoStrings.CardMultiNumber, allTasksCount.ToString()) : string.Format(ToDoStrings.CardOneNumber, allTasksCount.ToString()),
                ToDos = todos,
                UseFile = useFile,
                CheckIconUrl = useFile ? GetImageUri(IconImageSource.CheckIconFile) : IconImageSource.CheckIconSource,
                UnCheckIconUrl = useFile ? GetImageUri(IconImageSource.UncheckIconFile) : IconImageSource.UncheckIconSource
            });
            return activity;
        }

        protected Activity ToAdaptiveCardForTaskDeletedFlowByLG(
            ITurnContext turnContext,
            List<TaskItem> todos,
            int allTasksCount,
            string taskContent,
            string listType,
            bool isDeleteAll)
        {
            bool useFile = Channel.GetChannelId(turnContext) == Channels.Msteams;

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.TaskDeleted, new
            {
                IsDeleteAll = isDeleteAll,
                ListType = listType,
                TaskContent = taskContent,
                Title = string.Format(ToDoStrings.CardTitle, listType),
                TotalNumber = allTasksCount > 1 ? string.Format(ToDoStrings.CardMultiNumber, allTasksCount.ToString()) : string.Format(ToDoStrings.CardOneNumber, allTasksCount.ToString()),
                ToDos = todos,
                UseFile = useFile,
                CheckIconUrl = useFile ? GetImageUri(IconImageSource.CheckIconFile) : IconImageSource.CheckIconSource,
                UnCheckIconUrl = useFile ? GetImageUri(IconImageSource.UncheckIconFile) : IconImageSource.UncheckIconSource
            });
            return activity;
        }

        protected Activity ToAdaptiveCardForDeletionRefusedFlowByLG(
            ITurnContext turnContext,
            List<TaskItem> todos,
            int allTasksCount,
            string listType)
        {
            bool useFile = Channel.GetChannelId(turnContext) == Channels.Msteams;

            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.DeletionAllConfirmationRefused, new
            {
                TaskCount = allTasksCount,
                ListType = listType,
                Title = string.Format(ToDoStrings.CardTitle, listType),
                TotalNumber = allTasksCount > 1 ? string.Format(ToDoStrings.CardMultiNumber, allTasksCount.ToString()) : string.Format(ToDoStrings.CardOneNumber, allTasksCount.ToString()),
                ToDos = todos,
                UseFile = useFile,
                CheckIconUrl = useFile ? GetImageUri(IconImageSource.CheckIconFile) : IconImageSource.CheckIconSource,
                UnCheckIconUrl = useFile ? GetImageUri(IconImageSource.UncheckIconFile) : IconImageSource.UncheckIconSource
            });
            return activity;
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ToDoErrorMessage);
            await sc.Context.SendActivityAsync(activity);

            // clear state
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        // This method is called by any waterfall step that throws a SkillException to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, SkillException ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            if (ex.ExceptionType == SkillExceptionType.APIAccessDenied)
            {
                var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ToDoErrorMessageBotProblem);
                await sc.Context.SendActivityAsync(activity);
            }
            else if (ex.ExceptionType == SkillExceptionType.AccountNotActivated)
            {
                var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ToDoErrorMessageAccountProblem);
                await sc.Context.SendActivityAsync(activity);
            }
            else
            {
                var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ToDoErrorMessage);
                await sc.Context.SendActivityAsync(activity);
            }

            // clear state
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        protected async Task<ITaskService> InitListTypeIds(WaterfallStepContext sc)
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);

            if (!state.ListTypeIds.ContainsKey(state.ListType))
            {
                var emailService = ServiceManager.InitMailService(token as string);
                var senderMailAddress = await emailService.GetSenderMailAddressAsync();
                state.UserStateId = senderMailAddress;
                var recovered = await RecoverListTypeIdsAsync(sc);
                if (!recovered)
                {
                    var taskServiceInit = ServiceManager.InitTaskService(token as string, state.ListTypeIds, state.TaskServiceType);
                    if (taskServiceInit.IsListCreated)
                    {
                        if (state.TaskServiceType == ServiceProviderType.OneNote)
                        {
                            var settingActivity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.SettingUpOneNoteMessage);
                            await sc.Context.SendActivityAsync(settingActivity);

                            var afterSettingActivity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.AfterOneNoteSetupMessage);
                            await sc.Context.SendActivityAsync(afterSettingActivity);
                        }
                        else
                        {
                            var settingActivity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.SettingUpOutlookMessage);
                            await sc.Context.SendActivityAsync(settingActivity);

                            var afterSettingActivity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.AfterOutlookSetupMessage);
                            await sc.Context.SendActivityAsync(afterSettingActivity);
                        }

                        var taskWebLink = await taskServiceInit.GetTaskWebLink();
                        var emailContent = string.Format(ToDoStrings.EmailContent, taskWebLink, taskWebLink);
                        await emailService.SendMessageAsync(emailContent, ToDoStrings.EmailSubject);
                    }

                    await StoreListTypeIdsAsync(sc);
                    return taskServiceInit;
                }
            }

            var taskService = ServiceManager.InitTaskService(token as string, state.ListTypeIds, state.TaskServiceType);
            await StoreListTypeIdsAsync(sc);
            return taskService;
        }

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }

        protected Task SendActionEnded(ITurnContext turnContext)
        {
            if (turnContext.IsSkill())
            {
                return Task.CompletedTask;
            }
            else
            {
                var activity = TemplateEngine.GenerateActivityForLocale(ToDoSharedResponses.ActionEnded);
                return turnContext.SendActivityAsync(activity);
            }
        }

        private async Task<bool> RecoverListTypeIdsAsync(DialogContext dc)
        {
            var userState = await UserStateAccessor.GetAsync(dc.Context, () => new ToDoSkillUserState());
            var state = await ToDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
            var senderMailAddress = state.UserStateId;
            if (userState.ListTypeIds.ContainsKey(senderMailAddress)
                && state.ListTypeIds.Count <= 0
                && userState.ListTypeIds[senderMailAddress].Count > 0)
            {
                foreach (var listType in userState.ListTypeIds[senderMailAddress])
                {
                    state.ListTypeIds.Add(listType.Key, listType.Value);
                }

                return true;
            }

            return false;
        }

        private async Task StoreListTypeIdsAsync(DialogContext dc)
        {
            var userState = await UserStateAccessor.GetAsync(dc.Context, () => new ToDoSkillUserState());
            var state = await ToDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
            var senderMailAddress = state.UserStateId;
            if (!userState.ListTypeIds.ContainsKey(senderMailAddress))
            {
                userState.ListTypeIds.Add(senderMailAddress, new Dictionary<string, string>());
                foreach (var listType in state.ListTypeIds)
                {
                    userState.ListTypeIds[senderMailAddress].Add(listType.Key, listType.Value);
                }
            }
            else
            {
                foreach (var listType in state.ListTypeIds)
                {
                    if (userState.ListTypeIds[senderMailAddress].ContainsKey(listType.Key))
                    {
                        userState.ListTypeIds[senderMailAddress][listType.Key] = listType.Value;
                    }
                    else
                    {
                        userState.ListTypeIds[senderMailAddress].Add(listType.Key, listType.Value);
                    }
                }
            }
        }

        private string GetImageUri(string imagePath)
        {
            var serverUrl = _httpContext.HttpContext.Request.Scheme + "://" + _httpContext.HttpContext.Request.Host.Value;
            return $"{serverUrl}/images/{imagePath}";
        }
    }
}