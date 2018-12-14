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
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Newtonsoft.Json.Linq;
using ToDoSkill.Dialogs.Shared.Resources;
using ToDoSkill.Dialogs.ShowToDo.Resources;

namespace ToDoSkill
{
    public class ToDoSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";

        public ToDoSkillDialog(
            string dialogId,
            ISkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> accessor,
            ITaskService serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            Accessor = accessor;
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

        protected ISkillConfiguration Services { get; set; }

        protected IStatePropertyAccessor<ToDoSkillState> Accessor { get; set; }

        protected ITaskService ServiceManager { get; set; }

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
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                    var state = await Accessor.GetAsync(sc.Context);
                    state.MsGraphToken = providerTokenResponse.TokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        protected async Task<DialogTurnResult> ClearContext(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
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

        protected async Task<DialogTurnResult> InitAllTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
            state.ListType = state.ListType ?? ToDoStrings.ToDo;

            if (!state.ListTypeIds.ContainsKey(state.ListType))
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOneNoteMessage));
                var service = await ServiceManager.InitAsync(state.MsGraphToken, state.ListTypeIds);
                state.AllTasks = await service.GetTasksAsync(state.ListType);
                state.ShowTaskPageIndex = 0;
                var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                state.Tasks = state.AllTasks.GetRange(0, rangeCount);
            }
            else if (state.ListType != state.LastListType)
            {
                // LastListType is used to switch between list types in DeleteToDoItemDialog and MarkToDoItemDialog.
                var service = await ServiceManager.InitAsync(state.MsGraphToken, state.ListTypeIds);
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

        protected async Task<DialogTurnResult> CollectToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectToDoTaskIndex);
        }

        protected async Task<DialogTurnResult> AskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
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
                var prompt = sc.Context.Activity.CreateReply(ToDoSharedResponses.AskToDoTaskIndex);
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
        }

        protected async Task<DialogTurnResult> AfterAskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context);
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

        protected async Task<DialogTurnResult> CollectToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectToDoTaskContent);
        }

        protected async Task<DialogTurnResult> AskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await this.Accessor.GetAsync(sc.Context);
            if (!string.IsNullOrEmpty(state.TaskContentPattern)
                || !string.IsNullOrEmpty(state.TaskContentML)
                || !string.IsNullOrEmpty(state.ShopContent))
            {
                return await sc.NextAsync();
            }
            else
            {
                var prompt = sc.Context.Activity.CreateReply(ToDoSharedResponses.AskToDoContentText);
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
        }

        protected async Task<DialogTurnResult> AfterAskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
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
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        protected async Task<DialogTurnResult> AddToDoTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                state.ListType = state.ListType ?? ToDoStrings.ToDo;
                state.LastListType = state.ListType;
                if (!state.ListTypeIds.ContainsKey(state.ListType))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOneNoteMessage));
                }

                var service = await ServiceManager.InitAsync(state.MsGraphToken, state.ListTypeIds);
                await service.AddTaskAsync(state.ListType, state.TaskContent);
                state.AllTasks = await service.GetTasksAsync(state.ListType);
                state.ShowTaskPageIndex = 0;
                var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                state.Tasks = state.AllTasks.GetRange(0, rangeCount);
                var toDoListAttachment = ToAdaptiveCardForOtherFlows(
                    state.Tasks,
                    state.AllTasks.Count,
                    state.TaskContent,
                    ToDoSharedResponses.AfterToDoTaskAdded,
                    ToDoSharedResponses.ShowToDoTasks);

                var toDoListReply = sc.Context.Activity.CreateReply();
                toDoListReply.Attachments.Add(toDoListAttachment);
                await sc.Context.SendActivityAsync(toDoListReply);
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
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
                var state = await Accessor.GetAsync(dc.Context);
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
           int allTasksCount)
        {
            var toDoCard = new AdaptiveCard();
            var speakText = Format(ToDoSharedResponses.ShowToDoTasks.Reply.Speak, new StringDictionary() { { "taskCount", allTasksCount.ToString() } })
                + Format(ShowToDoResponses.FirstToDoTasks.Reply.Speak, new StringDictionary() { { "taskCount", toBeReadTasksCount.ToString() } });
            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.ShowToDoTasks.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() } });
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
           int allTasksCount)
        {
            var toDoCard = new AdaptiveCard();
            var body = new List<AdaptiveElement>();
            var showText = Format(ToDoSharedResponses.ShowToDoTasks.Reply.Text, new StringDictionary() { { "taskCount", allTasksCount.ToString() } });
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
            var toDoCard = new AdaptiveCard();
            var speakText = Format(ShowToDoResponses.ShowNextToDoTasks.Reply.Speak, new StringDictionary() { })
                + Format(ShowToDoResponses.FirstToDoTasks.Reply.Speak, new StringDictionary() { { "taskCount", toBeReadTasksCount.ToString() } });
            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var showText = Format(ShowToDoResponses.ShowNextToDoTasks.Reply.Text, new StringDictionary() { });
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
            var toDoCard = new AdaptiveCard();
            var speakText = Format(ShowToDoResponses.ShowPreviousToDoTasks.Reply.Speak, new StringDictionary() { })
                + Format(ShowToDoResponses.FirstToDoTasks.Reply.Speak, new StringDictionary() { { "taskCount", toBeReadTasksCount.ToString() } });
            toDoCard.Speak = speakText;

            var body = new List<AdaptiveElement>();
            var showText = Format(ShowToDoResponses.ShowPreviousToDoTasks.Reply.Text, new StringDictionary() { });
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
            BotResponse botResponse1,
            BotResponse botResponse2)
        {
            var toDoCard = new AdaptiveCard();
            var showText = Format(botResponse2.Reply.Text, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            var speakText = Format(botResponse1.Reply.Speak, new StringDictionary() { { "taskContent", taskContent } })
                + Format(botResponse2.Reply.Speak, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            toDoCard.Speak = speakText;

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

        protected string Format(string messageTemplate, StringDictionary tokens)
        {
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

        protected async Task HandleDialogExceptions(WaterfallStepContext sc)
        {
            var state = await Accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();
        }

        private void ExtractListTypeAndTaskContent(ToDoSkillState state)
        {
            if (state.ListType == ToDoStrings.Grocery
                || (state.HasShopVerb && !string.IsNullOrEmpty(state.FoodOfGrocery)))
            {
                state.ListType = ToDoStrings.Grocery;
                state.TaskContent = string.IsNullOrEmpty(state.ShopContent) ? state.TaskContentML ?? state.TaskContentPattern : state.ShopContent;
            }
            else if (state.ListType == ToDoStrings.Shopping
                || (state.HasShopVerb && !string.IsNullOrEmpty(state.ShopContent)))
            {
                state.ListType = ToDoStrings.Shopping;
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