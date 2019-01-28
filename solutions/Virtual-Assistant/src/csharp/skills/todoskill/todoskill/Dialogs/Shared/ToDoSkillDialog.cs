using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Middleware.Telemetry;
using Microsoft.Bot.Solutions.Prompts;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Newtonsoft.Json.Linq;
using ToDoSkill.Dialogs.AddToDo.Resources;
using ToDoSkill.Dialogs.DeleteToDo.Resources;
using ToDoSkill.Dialogs.MarkToDo.Resources;
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
            IStatePropertyAccessor<ToDoSkillState> toDoStateAccessor,
            IStatePropertyAccessor<ToDoSkillUserState> userStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
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

        protected ToDoSkillResponseBuilder ResponseBuilder { get; set; }

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
                    return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = sc.Context.Activity.CreateReply(ToDoSharedResponses.NoAuth, ResponseBuilder) });
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
                else if (generalTopIntent == General.Intent.Previous && state.ShowTaskPageIndex > 0)
                {
                    state.ReadTaskIndex = 0;
                    state.ShowTaskPageIndex--;
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
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.NoTasksInList));
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
                    int index = 0;
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
                    if (ToDoStrings.GrocerySynonym.Contains(entities.ListType[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.ListType = ToDoStrings.Grocery;
                    }
                    else if (ToDoStrings.ShoppingSynonym.Contains(entities.ListType[0], StringComparison.InvariantCultureIgnoreCase))
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
           int allTasksCount,
           int readSize,
           string listType)
        {
            var toDoCard = new AdaptiveCard();

            var speakText = Format(ShowToDoResponses.TaskSummaryMessage.Reply.Speak, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } }) + " ";
            if (todos.Count == 1)
            {
                speakText += Format(ShowToDoResponses.LatestOneTask.Reply.Speak) + " ";
            }
            else if (todos.Count == 2)
            {
                speakText += Format(ShowToDoResponses.LatestTwoTasks.Reply.Speak) + " ";
            }
            else if (todos.Count >= readSize)
            {
                speakText += Format(ShowToDoResponses.LatestThreeOrMoreTasks.Reply.Speak, new StringDictionary() { { "taskCount", readSize.ToString() } }) + " ";
            }

            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.CardSummaryMessage.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            int index = 0;
            readSize = Math.Min(readSize, todos.Count);
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage();
                icon.UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource;
                var iconColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);

                if (index < readSize)
                {
                    if (readSize == 1)
                    {
                        toDoCard.Speak += todo.Topic + ". ";
                    }
                    else if (index == readSize - 2)
                    {
                        toDoCard.Speak += todo.Topic;
                    }
                    else if (index == readSize - 1)
                    {
                        toDoCard.Speak += string.Format(ToDoStrings.And, todo.Topic);
                    }
                    else
                    {
                        toDoCard.Speak += todo.Topic + ", ";
                    }
                }

                index++;
            }

            if (todos.Count <= readSize)
            {
                toDoCard.Speak += Format(ShowToDoResponses.AskAddOrCompleteTaskMessage.Reply.Speak);
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
            var toDoCard = new AdaptiveCard();

            if (toBeReadTasksCount == 1)
            {
                toDoCard.Speak = Format(ShowToDoResponses.NextOneTask.Reply.Speak) + " ";
            }
            else if (toBeReadTasksCount == 2)
            {
                toDoCard.Speak += Format(ShowToDoResponses.NextTwoTasks.Reply.Speak) + " ";
            }
            else
            {
                toDoCard.Speak += Format(ShowToDoResponses.NextThreeOrMoreTask.Reply.Speak, new StringDictionary() { { "taskCount", toBeReadTasksCount.ToString() } }) + " ";
            }

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.CardSummaryMessage.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            int index = 0;
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage();
                icon.UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource;
                var iconColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);
                if (index >= startIndexOfTasksToBeRead && index < startIndexOfTasksToBeRead + toBeReadTasksCount)
                {
                    if (toBeReadTasksCount == 1)
                    {
                        toDoCard.Speak += todo.Topic + ". ";
                    }
                    else if (index == startIndexOfTasksToBeRead + toBeReadTasksCount - 2)
                    {
                        toDoCard.Speak += todo.Topic;
                    }
                    else if (index == startIndexOfTasksToBeRead + toBeReadTasksCount - 1)
                    {
                        toDoCard.Speak += string.Format(ToDoStrings.And, todo.Topic);
                    }
                    else
                    {
                        toDoCard.Speak += todo.Topic + ", ";
                    }
                }

                index++;
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
           int readSize,
           int allTasksCount,
           string listType)
        {
            var toDoCard = new AdaptiveCard();
            toDoCard.Speak = Format(ShowToDoResponses.ShowPreviousTasks.Reply.Speak, new StringDictionary()) + " ";

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.CardSummaryMessage.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            readSize = Math.Min(readSize, todos.Count);
            var index = 0;
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage();
                icon.UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource;
                var iconColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                iconColumn.Items.Add(icon);
                columnSet.Columns.Add(iconColumn);

                var content = new AdaptiveTextBlock(todo.Topic);
                var contentColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
                contentColumn.Items.Add(content);
                columnSet.Columns.Add(contentColumn);

                container.Items.Add(columnSet);

                if (index < readSize)
                {
                    if (readSize == 1)
                    {
                        toDoCard.Speak += todo.Topic + ". ";
                    }
                    else if (index == readSize - 2)
                    {
                        toDoCard.Speak += todo.Topic;
                    }
                    else if (index == readSize - 1)
                    {
                        toDoCard.Speak += string.Format(ToDoStrings.And, todo.Topic);
                    }
                    else
                    {
                        toDoCard.Speak += todo.Topic + ", ";
                    }
                }

                index++;
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

        protected Attachment ToAdaptiveCardForTaskAddedFlow(
           List<TaskItem> todos,
           string taskContent,
           int allTasksCount,
           string listType)
        {
            var toDoCard = new AdaptiveCard();
            toDoCard.Speak = Format(AddToDoResponses.AfterTaskAdded.Reply.Speak, new StringDictionary() { { "taskContent", taskContent }, { "listType", listType } });

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.CardSummaryMessage.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage();
                icon.UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource;
                var iconColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
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

        protected Attachment ToAdaptiveCardForTaskCompletedFlow(
            List<TaskItem> todos,
            int allTasksCount,
            string taskContent,
            string listType,
            bool isCompleteAll)
        {
            var toDoCard = new AdaptiveCard();

            if (isCompleteAll)
            {
                toDoCard.Speak = Format(MarkToDoResponses.AfterAllTasksCompleted.Reply.Speak, new StringDictionary() { { "listType", listType } });
            }
            else
            {
                toDoCard.Speak = Format(MarkToDoResponses.AfterTaskCompleted.Reply.Speak, new StringDictionary() { { "taskContent", taskContent }, { "listType", listType } });
            }

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.CardSummaryMessage.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage();
                icon.UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource;
                var iconColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
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

        protected Attachment ToAdaptiveCardForTaskDeletedFlow(
            List<TaskItem> todos,
            int allTasksCount,
            string taskContent,
            string listType,
            bool isDeleteAll)
        {
            var toDoCard = new AdaptiveCard();

            if (isDeleteAll)
            {
                toDoCard.Speak = Format(DeleteToDoResponses.AfterAllTasksDeleted.Reply.Speak, new StringDictionary() { { "listType", listType } });
            }
            else
            {
                toDoCard.Speak = Format(DeleteToDoResponses.AfterTaskDeleted.Reply.Speak, new StringDictionary() { { "taskContent", taskContent }, { "listType", listType } });
            }

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.CardSummaryMessage.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage();
                icon.UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource;
                var iconColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
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

        protected Attachment ToAdaptiveCardForDeletionRefusedFlow(
            List<TaskItem> todos,
            int allTasksCount,
            string listType)
        {
            var toDoCard = new AdaptiveCard();

            toDoCard.Speak = Format(DeleteToDoResponses.DeletionAllConfirmationRefused.Reply.Speak, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.CardSummaryMessage.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() }, { "listType", listType } });
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);

            var container = new AdaptiveContainer();
            foreach (var todo in todos)
            {
                var columnSet = new AdaptiveColumnSet();

                var icon = new AdaptiveImage();
                icon.UrlString = todo.IsCompleted ? IconImageSource.CheckIconSource : IconImageSource.UncheckIconSource;
                var iconColumn = new AdaptiveColumn();
                iconColumn.Width = "auto";
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

        protected string GenerateResponseWithTokens(BotResponse botResponse, StringDictionary tokens)
        {
            return Format(botResponse.Reply.Text, tokens);
        }

        protected string Format(string messageTemplate, StringDictionary tokens = null)
        {
            if (tokens == null)
            {
                tokens = new StringDictionary() { };
            }

            var complexTokensRegex = new Regex(@"\{[^{\}]+(?=})\}", RegexOptions.Compiled);
            var responseFormatters = new List<IBotResponseFormatter>();
            var defaultFormatter = new DefaultBotResponseFormatter();

            var result = messageTemplate;
            var matches = complexTokensRegex.Matches(messageTemplate);
            foreach (var match in matches)
            {
                var formatted = false;
                var bindingJson = match.ToString();
                foreach (var formatter in responseFormatters)
                {
                    if (formatter.CanFormat(bindingJson))
                    {
                        result = formatter.FormatResponse(result, bindingJson, tokens);
                        formatted = true;
                        break;
                    }
                }

                if (!formatted)
                {
                    result = defaultFormatter.FormatResponse(result, bindingJson, tokens);
                }
            }

            return result;
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
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ToDoErrorMessage));

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
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ToDoErrorMessage_BotProblem));
            }
            else
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.ToDoErrorMessage));
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
                state.UserStateId = senderMailAddress;
                var recovered = await RecoverListTypeIdsAsync(sc);
                if (!recovered)
                {
                    if (state.TaskServiceType == ProviderTypes.OneNote)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOneNoteMessage));
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOutlookMessage));
                    }

                    var taskServiceInit = ServiceManager.InitTaskService(state.MsGraphToken, state.ListTypeIds, state.TaskServiceType);
                    var taskWebLink = await taskServiceInit.GetTaskWebLink();
                    var emailContent = string.Format(ToDoStrings.EmailContent, taskWebLink);
                    await emailService.SendMessageAsync(emailContent, ToDoStrings.EmailSubject);

                    if (state.TaskServiceType == ProviderTypes.OneNote)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.AfterOneNoteSetupMessage));
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.AfterOutlookSetupMessage));
                    }

                    await StoreListTypeIdsAsync(sc);
                    return taskServiceInit;
                }
            }

            var taskService = ServiceManager.InitTaskService(state.MsGraphToken, state.ListTypeIds, state.TaskServiceType);
            await StoreListTypeIdsAsync(sc);
            return taskService;
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
    }
}