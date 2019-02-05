using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Prompts;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Newtonsoft.Json.Linq;
using ToDoSkill.Dialogs.Shared.DialogOptions;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;
using ToDoSkill.Models;
using ToDoSkill.ServiceClients;
using static ToDoSkill.Dialogs.Shared.ServiceProviderTypes;

namespace ToDoSkill.Dialogs.Shared
{
    public class ToDoSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";

        public ToDoSkillDialog(
            string dialogId,
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            ResponseManager = responseManager;
            ToDoStateAccessor = toDoStateAccessor;
            UserStateAccessor = userStateAccessor;
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;

            if (!Services.AuthenticationConnections.Any())
            {
                throw new Exception("You must configure an authentication connection in your bot file before using this component.");
            }

            AddDialog(new EventPrompt(SkillModeAuth, "tokens/response", TokenResponseValidator));
            AddDialog(new MultiProviderAuthDialog(services));
            AddDialog(new TextPrompt(Action.Prompt));
        }

        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<ToDoSkillState> ToDoStateAccessor { get; set; }

        protected IStatePropertyAccessor<ToDoSkillUserState> UserStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

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
            if (!string.IsNullOrWhiteSpace(resultString) && resultString.Equals(CommonUtil.DialogTurnResultCancelAllDialogs, StringComparison.InvariantCultureIgnoreCase))
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
                var skillOptions = (ToDoSkillDialogOptions)sc.Options;

                // If in Skill mode we ask the calling Bot for the token
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
                    // TODO Error handling - if we get a new activity that isn't an event
                    var response = sc.Context.Activity.CreateReply();
                    response.Type = ActivityTypes.Event;
                    response.Name = "tokens/request";

                    // Send the tokens/request Event
                    await sc.Context.SendActivityAsync(response);

                    // Wait for the tokens/response event
                    return await sc.PromptAsync(SkillModeAuth, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = ResponseManager.GetResponse(ToDoSharedResponses.NoAuth) });
                }
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
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (ToDoSkillDialogOptions)sc.Options;
                ProviderTokenResponse providerTokenResponse;
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    var resultType = sc.Context.Activity.Value.GetType();
                    if (resultType == typeof(ProviderTokenResponse))
                    {
                        providerTokenResponse = sc.Context.Activity.Value as ProviderTokenResponse;
                    }
                    else
                    {
                        var tokenResponseObject = sc.Context.Activity.Value as JObject;
                        providerTokenResponse = tokenResponseObject?.ToObject<ProviderTokenResponse>();
                    }
                }
                else
                {
                    providerTokenResponse = sc.Result as ProviderTokenResponse;
                }

                if (providerTokenResponse != null)
                {
                    var state = await ToDoStateAccessor.GetAsync(sc.Context);
                    state.MsGraphToken = providerTokenResponse.TokenResponse.Token;
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
                var topIntent = state.LuisResult?.TopIntent().intent;
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                if (topIntent == ToDo.Intent.ShowToDo)
                {
                    state.ShowTaskPageIndex = 0;
                    state.ReadTaskIndex = 0;
                    state.Tasks = new List<TaskItem>();
                    state.AllTasks = new List<TaskItem>();
                    state.ListType = null;
                    await DigestToDoLuisResult(sc);
                }
                else if (generalTopIntent == General.Intent.Next)
                {
                    state.ReadTaskIndex = 0;
                    if ((state.ShowTaskPageIndex + 1) * state.PageSize < state.AllTasks.Count)
                    {
                        state.ShowTaskPageIndex++;
                    }
                }
                else if (generalTopIntent == General.Intent.Previous && state.ShowTaskPageIndex > 0)
                {
                    state.ReadTaskIndex = 0;
                    state.ShowTaskPageIndex--;
                }
                else if (generalTopIntent == General.Intent.ReadMore)
                {
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
                }
                else if (topIntent == ToDo.Intent.AddToDo)
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
                else if (topIntent == ToDo.Intent.MarkToDo || topIntent == ToDo.Intent.DeleteToDo)
                {
                    state.TaskIndexes = new List<int>();
                    state.MarkOrDeleteAllTasksFlag = false;
                    state.TaskContentPattern = null;
                    state.TaskContentML = null;
                    state.TaskContent = null;
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
                state.ListType = state.ListType ?? ToDoStrings.ToDo;

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
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.NoTasksInList));
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

        protected async Task<DialogTurnResult> CollectToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectToDoTaskIndex);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    var prompt = ResponseManager.GetResponse(ToDoSharedResponses.AskToDoTaskIndex);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    return await sc.BeginDialogAsync(Action.CollectToDoTaskIndex);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Action.CollectToDoTaskContent);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    var prompt = ResponseManager.GetResponse(ToDoSharedResponses.AskToDoContentText);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterAskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                        return await sc.BeginDialogAsync(Action.CollectToDoTaskContent);
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
                var prompt = ResponseManager.GetResponse(ToDoSharedResponses.SwitchListType, token);
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
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    state.ListType = state.LastListType;
                    state.LastListType = null;
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.CollectSwitchListTypeConfirmation);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AddToDoTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await ToDoStateAccessor.GetAsync(sc.Context);
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                state.LastListType = state.ListType;
                var service = await InitListTypeIds(sc);
                await service.AddTaskAsync(state.ListType, state.TaskContent);
                state.AllTasks = await service.GetTasksAsync(state.ListType);
                state.ShowTaskPageIndex = 0;
                var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                state.Tasks = state.AllTasks.GetRange(0, rangeCount);
                var toDoListAttachment = ToAdaptiveCardForOtherFlows(
                    state.Tasks,
                    state.AllTasks.Count,
                    state.TaskContent,
                    ResponseManager.GetResponseTemplate(ToDoSharedResponses.AfterToDoTaskAdded),
                    ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks),
                    state.ListType);

                var toDoListReply = sc.Context.Activity.CreateReply();
                toDoListReply.Attachments.Add(toDoListAttachment);
                await sc.Context.SendActivityAsync(toDoListReply);
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
                var luisResult = state.LuisResult;
                var entities = luisResult.Entities;
                if (entities.ContainsAll != null)
                {
                    state.MarkOrDeleteAllTasksFlag = true;
                }

                if (entities.ordinal != null || entities.number != null)
                {
                    var indexOfOrdinal = entities.ordinal == null ? 0 : (int)entities.ordinal[0];
                    var indexOfNumber = entities.number == null ? 0 : (int)entities.number[0];
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
                    if (entities.ListType[0].Equals(ToDoStrings.Grocery, StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.ListType = ToDoStrings.Grocery;
                    }
                    else if (entities.ListType[0].Equals(ToDoStrings.Shopping, StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.ListType = ToDoStrings.Shopping;
                    }
                    else
                    {
                        state.ListType = ToDoStrings.ToDo;
                    }
                }

                if (entities.FoodOfGrocery != null)
                {
                    state.FoodOfGrocery = entities.FoodOfGrocery[0][0];
                }

                if (entities.ShopVerb != null && (entities.ShopContent != null || entities.FoodOfGrocery != null))
                {
                    state.HasShopVerb = true;
                }

                if (entities.ShopContent != null)
                {
                    state.ShopContent = entities.ShopContent[0];
                }

                if (entities.TaskContentPattern != null)
                {
                    state.TaskContentPattern = entities.TaskContentPattern[0];
                }

                if (entities.TaskContentML != null)
                {
                    state.TaskContentML = entities.TaskContentML[0];
                }

                if (dc.Context.Activity.Text != null)
                {
                    var words = dc.Context.Activity.Text.Split(' ');
                    foreach (var word in words)
                    {
                        if (word.Equals("all", StringComparison.OrdinalIgnoreCase))
                        {
                            state.MarkOrDeleteAllTasksFlag = true;
                        }
                    }
                }
            }
            catch
            {
                // ToDo
            }
        }

        protected Attachment ToAdaptiveCardForShowToDos(
           List<TaskItem> todos,
           int toBeReadTasksCount,
           int allTasksCount,
           string listType)
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
            var toDoCard = new AdaptiveCard();
            var speakText = ResponseManager.Format(response.Reply.Speak, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } })
                + ResponseManager.Format(response.Reply.Speak, new StringDictionary() { { "taskCount", toBeReadTasksCount.ToString() } });
            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var showText = ResponseManager.Format(response.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            var index = 0;
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage
                {
                    UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource
                };
                var iconColumn = new AdaptiveColumn
                {
                    Width = "auto"
                };
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);

                if (index < toBeReadTasksCount)
                {
                    toDoCard.Speak += (++index) + " " + todo.Topic + " ";
                }
            }

            body.Add(container);
            toDoCard.Body = body;

            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        protected Attachment ToAdaptiveCardForReadMore(
           List<TaskItem> todos,
           int startIndexOfTasksToBeRead,
           int toBeReadTasksCount,
           int allTasksCount,
           string listType)
        {
            var response = ResponseManager.GetResponseTemplate(ToDoSharedResponses.ShowToDoTasks);
            var toDoCard = new AdaptiveCard();
            var body = new List<AdaptiveElement>();
            var showText = ResponseManager.Format(
                response.Reply.Text,
                new StringDictionary()
                {
                    { "taskCount", allTasksCount.ToString() },
                    { "listType", listType }
                });

            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            var index = 0;
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage
                {
                    UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource
                };
                var iconColumn = new AdaptiveColumn
                {
                    Width = "auto"
                };
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);

                index++;
                if (index > startIndexOfTasksToBeRead && index <= toBeReadTasksCount + startIndexOfTasksToBeRead)
                {
                    toDoCard.Speak += index + " " + todo.Topic + " ";
                }
            }

            body.Add(container);
            toDoCard.Body = body;

            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        protected Attachment ToAdaptiveCardForNextPage(
           List<TaskItem> todos,
           int toBeReadTasksCount)
        {
            var response = ResponseManager.GetResponseTemplate(ShowToDoResponses.ShowNextToDoTasks);
            var toDoCard = new AdaptiveCard();
            var speakText = response.Reply.Speak
                + ResponseManager.Format(response.Reply.Speak, new StringDictionary() { { "taskCount", toBeReadTasksCount.ToString() } });
            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var showText = response.Reply.Text;
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            var index = 0;
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage
                {
                    UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource
                };
                var iconColumn = new AdaptiveColumn
                {
                    Width = "auto"
                };
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);

                if (index < toBeReadTasksCount)
                {
                    toDoCard.Speak += (++index) + " " + todo.Topic + " ";
                }
            }

            body.Add(container);
            toDoCard.Body = body;

            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        protected Attachment ToAdaptiveCardForPreviousPage(
           List<TaskItem> todos,
           int toBeReadTasksCount)
        {
            var response = ResponseManager.GetResponseTemplate(ShowToDoResponses.ShowPreviousToDoTasks);
            var toDoCard = new AdaptiveCard();
            var speakText = response.Reply.Speak 
                + ResponseManager.Format(response.Reply.Speak, new StringDictionary() { { "taskCount", toBeReadTasksCount.ToString() } });
            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var showText = response.Reply.Text;
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            var index = 0;
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage
                {
                    UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource
                };
                var iconColumn = new AdaptiveColumn
                {
                    Width = "auto"
                };
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);

                if (index < toBeReadTasksCount)
                {
                    toDoCard.Speak += (++index) + " " + todo.Topic + " ";
                }
            }

            body.Add(container);
            toDoCard.Body = body;

            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        protected Attachment ToAdaptiveCardForOtherFlows(
            List<TaskItem> todos,
            int allTaskCount,
            string taskContent,
            ResponseTemplate botResponse1,
            ResponseTemplate botResponse2,
            string listType)
        {
            var toDoCard = new AdaptiveCard();
            var showText = ResponseManager.Format(botResponse2.Reply.Text, new StringDictionary() { { "taskCount", allTaskCount.ToString() }, { "listType", listType } });
            var speakText = ResponseManager.Format(botResponse1.Reply.Speak, new StringDictionary() { { "taskContent", taskContent } })
                 + " " + ResponseManager.Format(botResponse2.Reply.Speak, new StringDictionary() { { "taskCount", allTaskCount.ToString() }, { "listType", listType } });
            toDoCard.Speak = speakText.Remove(speakText.Length - 1) + ".";

            var body = new List<AdaptiveElement>();
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage
                {
                    UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource
                };
                var iconColumn = new AdaptiveColumn
                {
                    Width = "auto"
                };
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);
            }

            body.Add(container);
            toDoCard.Body = body;

            var attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);

            // send error message to bot user
            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.ToDoErrorMessage));

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
            TelemetryClient.TrackExceptionEx(ex, sc.Context.Activity, sc.ActiveDialog?.Id);

            // send error message to bot user
            if (ex.ExceptionType == SkillExceptionType.APIAccessDenied)
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.ToDoErrorMessage_BotProblem));
            }
            else
            {
                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.ToDoErrorMessage));
            }

            // clear state
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            state.Clear();
        }

        protected async Task<ITaskService> InitListTypeIds(WaterfallStepContext sc)
        {
            var state = await ToDoStateAccessor.GetAsync(sc.Context);
            if (!state.ListTypeIds.ContainsKey(state.ListType))
            {
                var emailService = ServiceManager.InitMailService(state.MsGraphToken);
                var senderMailAddress = await emailService.GetSenderMailAddressAsync();
                var recovered = await RecoverListTypeIdsAsync(sc, senderMailAddress);
                if (!recovered)
                {
                    if (state.TaskServiceType == ProviderTypes.OneNote)
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.SettingUpOneNoteMessage));
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.SettingUpOutlookMessage));
                    }

                    var taskService = ServiceManager.InitTaskService(state.MsGraphToken, state.ListTypeIds, state.TaskServiceType);
                    var taskWebLink = await taskService.GetTaskWebLink();
                    var emailContent = string.Format(ToDoStrings.EmailContent, taskWebLink);
                    await emailService.SendMessageAsync(emailContent, ToDoStrings.EmailSubject);

                    if (state.TaskServiceType == ProviderTypes.OneNote)
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.AfterOneNoteSetupMessage));
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(ToDoSharedResponses.AfterOutlookSetupMessage));
                    }

                    await StoreListTypeIdsAsync(sc, senderMailAddress);
                    return taskService;
                }
            }

            return ServiceManager.InitTaskService(state.MsGraphToken, state.ListTypeIds, state.TaskServiceType);
        }

        private void ExtractListTypeAndTaskContent(ToDoSkillState state)
        {
            if (state.ListType == ToDoStrings.Grocery
                || (state.HasShopVerb && !string.IsNullOrEmpty(state.FoodOfGrocery)))
            {
                state.TaskContent = string.IsNullOrEmpty(state.ShopContent) ? state.TaskContentML ?? state.TaskContentPattern : state.ShopContent;
                if (state.ListType != ToDoStrings.Grocery)
                {
                    state.LastListType = state.ListType;
                    state.ListType = ToDoStrings.Grocery;
                    state.SwitchListType = true;
                }
                else
                {
                    state.ListType = ToDoStrings.Grocery;
                }
            }
            else if (state.ListType == ToDoStrings.Shopping
                || (state.HasShopVerb && !string.IsNullOrEmpty(state.ShopContent)))
            {
                state.TaskContent = string.IsNullOrEmpty(state.ShopContent) ? state.TaskContentML ?? state.TaskContentPattern : state.ShopContent;
                if (state.ListType != ToDoStrings.Shopping)
                {
                    state.LastListType = state.ListType;
                    state.ListType = ToDoStrings.Shopping;
                    state.SwitchListType = true;
                }
                else
                {
                    state.ListType = ToDoStrings.Shopping;
                }
            }
            else
            {
                state.ListType = ToDoStrings.ToDo;
                state.TaskContent = state.TaskContentML ?? state.TaskContentPattern;
            }
        }

        private async Task<bool> RecoverListTypeIdsAsync(DialogContext dc, string senderMailAddress)
        {
            var userState = await UserStateAccessor.GetAsync(dc.Context, () => new ToDoSkillUserState());
            var state = await ToDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
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

        private async Task StoreListTypeIdsAsync(DialogContext dc, string senderMailAddress)
        {
            var userState = await UserStateAccessor.GetAsync(dc.Context, () => new ToDoSkillUserState());
            var state = await ToDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
            if (!userState.ListTypeIds.ContainsKey(senderMailAddress) && state.ListTypeIds.Count > 0)
            {
                userState.ListTypeIds.Add(senderMailAddress, new Dictionary<string, string>());
                foreach (var listType in state.ListTypeIds)
                {
                    userState.ListTypeIds[senderMailAddress].Add(listType.Key, listType.Value);
                }
            }
        }
    }
}