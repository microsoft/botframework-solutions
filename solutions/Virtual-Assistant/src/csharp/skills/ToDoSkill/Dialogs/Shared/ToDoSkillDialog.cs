using AdaptiveCards;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkill
{
    public class ToDoSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";

        // Fields
        protected SkillConfiguration _services;
        protected IStatePropertyAccessor<ToDoSkillState> _accessor;
        protected IToDoService _serviceManager;
        protected ToDoSkillResponseBuilder _responseBuilder = new ToDoSkillResponseBuilder();

        public ToDoSkillDialog(
            string dialogId,
            SkillConfiguration services,
            IStatePropertyAccessor<ToDoSkillState> accessor,
            IToDoService serviceManager)
            : base(dialogId)
        {
            _services = services;
            _accessor = accessor;
            _serviceManager = serviceManager;

            var oauthSettings = new OAuthPromptSettings()
            {
                ConnectionName = _services.AuthConnectionName ?? throw new Exception("The authentication connection has not been initialized."),
                Text = $"Authentication",
                Title = "Signin",
                Timeout = 300000, // User has 5 minutes to login
            };

            AddDialog(new EventPrompt(SkillModeAuth, "tokens/response", TokenResponseValidator));
            AddDialog(new OAuthPrompt(LocalModeAuth, oauthSettings, AuthPromptValidator));
            AddDialog(new TextPrompt(Action.Prompt));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(dc.Context);
            await DigestToDoLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(dc.Context);
            await DigestToDoLuisResult(dc, state.LuisResult);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }


        // Shared steps
        public async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    return await sc.PromptAsync(LocalModeAuth, new PromptOptions());
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (ToDoSkillDialogOptions)sc.Options;
                TokenResponse tokenResponse;
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    var resultType = sc.Context.Activity.Value.GetType();
                    if (resultType == typeof(TokenResponse))
                    {
                        tokenResponse = sc.Context.Activity.Value as TokenResponse;
                    }
                    else
                    {
                        var tokenResponseObject = sc.Context.Activity.Value as JObject;
                        tokenResponse = tokenResponseObject?.ToObject<TokenResponse>();
                    }
                }
                else
                {
                    tokenResponse = sc.Result as TokenResponse;
                }

                if (tokenResponse != null)
                {
                    var state = await _accessor.GetAsync(sc.Context);
                    state.MsGraphToken = tokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> ClearContext(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(sc.Context);
            var topIntent = state.LuisResult?.TopIntent().intent;
            if (topIntent == ToDo.Intent.ShowToDo)
            {
                state.ShowToDoPageIndex = 0;
                state.Tasks = new List<ToDoItem>();
                state.AllTasks = new List<ToDoItem>();
            }
            else if (topIntent == ToDo.Intent.Next)
            {
                if ((state.ShowToDoPageIndex + 1) * state.PageSize < state.AllTasks.Count)
                {
                    state.ShowToDoPageIndex++;
                }
            }
            else if (topIntent == ToDo.Intent.Previous && state.ShowToDoPageIndex > 0)
            {
                state.ShowToDoPageIndex--;
            }
            else if (topIntent == ToDo.Intent.AddToDo)
            {
                state.TaskContent = null;
            }
            else if (topIntent == ToDo.Intent.MarkToDo || topIntent == ToDo.Intent.DeleteToDo)
            {
                state.TaskIndexes = new List<int>();
                state.MarkOrDeleteAllTasksFlag = false;
                state.TaskContent = null;
                await DigestToDoLuisResult(sc, state.LuisResult);
            }

            return await sc.NextAsync();
        }

        public async Task<DialogTurnResult> CollectToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectToDoTaskIndex);
        }

        public async Task<DialogTurnResult> AskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(sc.Context);
            if (!string.IsNullOrEmpty(state.TaskContent)
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

        public async Task<DialogTurnResult> AfterAskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(sc.Context);
            if (string.IsNullOrEmpty(state.TaskContent)
                && !state.MarkOrDeleteAllTasksFlag
                && (state.TaskIndexes.Count == 0
                    || state.TaskIndexes[0] < 0
                    || state.TaskIndexes[0] >= state.Tasks.Count))
            {
                await DigestToDoLuisResult(sc, state.LuisResult);
            }

            var matchedIndexes = Enumerable.Range(0, state.AllTasks.Count)
                .Where(i => state.AllTasks[i].Topic.Equals(state.TaskContent, StringComparison.OrdinalIgnoreCase))
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
                state.TaskIndexes[0] = (state.PageSize * state.ShowToDoPageIndex) + state.TaskIndexes[0];
                return await sc.EndDialogAsync(true);
            }
            else
            {
                state.TaskContent = null;
                return await sc.BeginDialogAsync(Action.CollectToDoTaskIndex);
            }
        }

        public async Task<DialogTurnResult> CollectToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectToDoTaskContent);
        }

        public async Task<DialogTurnResult> AskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await this._accessor.GetAsync(sc.Context);
            if (!string.IsNullOrEmpty(state.TaskContent))
            {
                return await sc.NextAsync();
            }
            else
            {
                var prompt = sc.Context.Activity.CreateReply(ToDoSharedResponses.AskToDoContentText);
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
        }

        public async Task<DialogTurnResult> AfterAskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.TaskContent))
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
                    return await sc.EndDialogAsync(true);
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AddToDoTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.OneNotePageId))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoSharedResponses.SettingUpOneNoteMessage));
                }

                var service = await _serviceManager.Init(state.MsGraphToken, state.OneNotePageId);
                var page = await service.GetDefaultToDoPage();
                state.OneNotePageId = page.Id;
                await service.AddToDo(state.TaskContent, page.ContentUrl);
                var todosAndPageIdTuple = await service.GetToDos();
                state.OneNotePageId = todosAndPageIdTuple.Item2;
                state.AllTasks = todosAndPageIdTuple.Item1;
                state.ShowToDoPageIndex = 0;
                var rangeCount = Math.Min(state.PageSize, state.AllTasks.Count);
                state.Tasks = state.AllTasks.GetRange(0, rangeCount);
                var toDoListAttachment = ToAdaptiveCardAttachmentForOtherFlows(
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
        private Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
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

        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> pc, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        // Helpers
        public async Task DigestToDoLuisResult(DialogContext dc, ToDo luisResult)
        {
            try
            {
                var state = await _accessor.GetAsync(dc.Context);
                var entities = luisResult.Entities;
                if (entities.ContainsAll != null)
                {
                    state.MarkOrDeleteAllTasksFlag = true;
                }

                if (entities.ordinal != null)
                {
                    var index = (int)entities.ordinal[0];
                    if (index > 0 && index <= 5)
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

                if (entities.TaskContent != null)
                {
                    state.TaskContent = entities.TaskContent[0];
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

        public static Microsoft.Bot.Schema.Attachment ToAdaptiveCardAttachmentForShowToDos(
           List<ToDoItem> todos,
           int allTaskCount,
           BotResponse botResponse1,
           BotResponse botResponse2)
        {
            var toDoCard = new AdaptiveCard();
            var speakText = Format(botResponse1.Reply.Speak, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            if (botResponse2 != null)
            {
                speakText += Format(botResponse2.Reply.Speak, new StringDictionary() { { "taskCount", todos.Count.ToString() } });
            }

            var showText = Format(botResponse1.Reply.Text, new StringDictionary() { { "taskCount", allTaskCount.ToString() } });
            toDoCard.Speak = speakText;
            var body = new List<AdaptiveElement>();
            var textBlock = new AdaptiveTextBlock
            {
                Text = showText,
            };
            body.Add(textBlock);
            var choiceSet = new AdaptiveChoiceSetInput
            {
                IsMultiSelect = true
            };
            var value = Guid.NewGuid().ToString() + ",";
            var index = 0;
            foreach (var todo in todos)
            {
                var choice = new AdaptiveChoice
                {
                    Title = todo.Topic,
                    Value = todo.Id
                };
                choiceSet.Choices.Add(choice);
                if (todo.IsCompleted)
                {
                    value += todo.Id + ",";
                }

                toDoCard.Speak += (++index) + "." + todo.Topic + " ";
            }

            value = value.Remove(value.Length - 1);
            choiceSet.Value = value;
            body.Add(choiceSet);
            toDoCard.Body = body;

            var attachment = new Microsoft.Bot.Schema.Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        public static Microsoft.Bot.Schema.Attachment ToAdaptiveCardAttachmentForOtherFlows(
            List<ToDoItem> todos,
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
            var choiceSet = new AdaptiveChoiceSetInput
            {
                IsMultiSelect = true
            };
            var value = Guid.NewGuid().ToString() + ",";
            foreach (var todo in todos)
            {
                var choice = new AdaptiveChoice
                {
                    Title = todo.Topic,
                    Value = todo.Id
                };
                choiceSet.Choices.Add(choice);
                if (todo.IsCompleted)
                {
                    value += todo.Id + ",";
                }
            }

            value = value.Remove(value.Length - 1);
            choiceSet.Value = value;
            body.Add(choiceSet);
            toDoCard.Body = body;

            var attachment = new Microsoft.Bot.Schema.Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = toDoCard,
            };
            return attachment;
        }

        public static string GenerateResponseWithTokens(BotResponse botResponse, StringDictionary tokens)
        {
            return Format(botResponse.Reply.Text, tokens);
        }

        private static string Format(string messageTemplate, StringDictionary tokens)
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

        public async Task HandleDialogExceptions(WaterfallStepContext sc)
        {
            var state = await _accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();
        }
    }
}
