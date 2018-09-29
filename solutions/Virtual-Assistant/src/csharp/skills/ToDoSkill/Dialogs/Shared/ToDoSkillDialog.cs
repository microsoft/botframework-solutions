// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Dialogs.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::ToDoSkill.Dialogs.AddToDoTask;
    using global::ToDoSkill.Dialogs.Shared.Resources;
    using global::ToDoSkill.Models;
    using global::ToDoSkill.ServiceClients;
    using Luis;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions;
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Extensions;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// bot steps need to be used is here.
    /// </summary>
    public class ToDoSkillDialog : ComponentDialog
    {
        private const string AuthSkillMode = "SkillAuth";
        private const string AuthLocalMode = "LocalAuth";
        private ToDoSkillServices toDoSkillServices;
        private ToDoSkillAccessors accessors;
        private IToDoService toDoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToDoSkillDialog"/> class.
        /// </summary>
        /// <param name="dialogId">Dialog id.</param>
        /// <param name="toDoSkillServices">To Do skill services.</param>
        /// <param name="accessors">To Do state accessors.</param>
        /// <param name="toDoService">To Do service.</param>
        public ToDoSkillDialog(string dialogId, ToDoSkillServices toDoSkillServices, ToDoSkillAccessors accessors, IToDoService toDoService)
            : base(dialogId)
        {
            this.toDoSkillServices = toDoSkillServices;
            this.accessors = accessors;
            this.toDoService = toDoService;

            var oauthSettings = new OAuthPromptSettings()
            {
                ConnectionName = this.toDoSkillServices.AuthConnectionName,
                Text = $"Authentication",
                Title = "Signin",
                Timeout = 300000,
            };

            this.AddDialog(new EventPrompt(AuthSkillMode, "tokens/response", this.TokenResponseValidator));
            this.AddDialog(new OAuthPrompt(AuthLocalMode, oauthSettings, this.AuthPromptValidator));
            this.AddDialog(new TextPrompt(Action.Prompt));
        }

        /// <summary>
        /// Get auth token step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                ToDoSkillDialogOptions skillOptions = (ToDoSkillDialogOptions)sc.Options;

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
                    return await sc.PromptAsync(AuthSkillMode, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(AuthLocalMode, new PromptOptions());
                }
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.AuthFailed));
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Get auth token step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
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
                    var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                    state.MsGraphToken = tokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.AuthFailed));
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Determine if clear the context.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> ClearContext(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
            var topIntent = ((ToDo)state.LuisResult)?.TopIntent().intent;
            if (topIntent == ToDo.Intent.ShowToDo)
            {
                state.ShowToDoPageIndex = 0;
                state.ToDoTaskActivities = new List<ToDoTaskActivityModel>();
                state.ToDoTaskAllActivities = new List<ToDoTaskActivityModel>();
            }
            else if (topIntent == ToDo.Intent.Next)
            {
                if ((state.ShowToDoPageIndex + 1) * state.PageSize < state.ToDoTaskAllActivities.Count)
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
                state.ToDoTaskContent = null;
                await ToDoHelper.DigestLuisResultAsync(sc.Context, this.accessors);
            }
            else if (topIntent == ToDo.Intent.MarkToDo || topIntent == ToDo.Intent.DeleteToDo)
            {
                state.ToDoTaskIndexes = new List<int>();
                state.MarkOrDeleteAllTasksFlag = false;
                state.ToDoTaskContent = null;
                await ToDoHelper.DigestLuisResultAsync(sc.Context, this.accessors);
            }

            return await sc.NextAsync();
        }

        /// <summary>
        /// Show To Do tasks step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> ShowToDoTasks(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.OneNotePageId))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.SettingUpOneNoteMessage));
                }

                var topIntent = ((ToDo)state.LuisResult)?.TopIntent().intent;
                if (topIntent == ToDo.Intent.ShowToDo || topIntent == ToDo.Intent.None)
                {
                    var service = await this.toDoService.Init(state.MsGraphToken, state.OneNotePageId);
                    var todosAndPageIdTuple = await service.GetMyToDoList();
                    state.OneNotePageId = todosAndPageIdTuple.Item2;
                    state.ToDoTaskAllActivities = todosAndPageIdTuple.Item1;
                }

                var allTasksCount = state.ToDoTaskAllActivities.Count;
                var currentTaskIndex = state.ShowToDoPageIndex * state.PageSize;
                state.ToDoTaskActivities = state.ToDoTaskAllActivities.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                if (state.ToDoTaskActivities.Count <= 0)
                {
                    return await sc.NextAsync();
                }
                else
                {
                    Attachment toDoListAttachment = null;
                    if (topIntent == ToDo.Intent.ShowToDo || topIntent == ToDo.Intent.None)
                    {
                        toDoListAttachment = ToDoHelper.ToAdaptiveCardAttachmentForShowToDos(
                            state.ToDoTaskActivities,
                            state.ToDoTaskAllActivities.Count,
                            ToDoBotResponses.ShowToDoTasks,
                            ToDoBotResponses.ReadToDoTasks);
                    }
                    else if (topIntent == ToDo.Intent.Next)
                    {
                        toDoListAttachment = ToDoHelper.ToAdaptiveCardAttachmentForShowToDos(
                            state.ToDoTaskActivities,
                            state.ToDoTaskAllActivities.Count,
                            ToDoBotResponses.ShowNextToDoTasks,
                            null);
                    }
                    else if (topIntent == ToDo.Intent.Previous)
                    {
                        toDoListAttachment = ToDoHelper.ToAdaptiveCardAttachmentForShowToDos(
                            state.ToDoTaskActivities,
                            state.ToDoTaskAllActivities.Count,
                            ToDoBotResponses.ShowPreviousToDoTasks,
                            null);
                    }

                    var toDoListReply = sc.Context.Activity.CreateReply();
                    toDoListReply.Attachments.Add(toDoListAttachment);
                    await sc.Context.SendActivityAsync(toDoListReply);
                    if ((topIntent == ToDo.Intent.ShowToDo || topIntent == ToDo.Intent.None)
                        && allTasksCount > (state.ShowToDoPageIndex + 1) * state.PageSize)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.ShowingMoreTasks));
                    }

                    return await sc.EndDialogAsync(true);
                }
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Ask confirmation if users want to add the first To Do task step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AskAddFirstTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var prompt = sc.Context.Activity.CreateReply(ToDoBotResponses.NoToDoTasksPrompt);
            return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
        }

        /// <summary>
        /// Check if confirmation is valid.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AfterAskAddFirstTaskConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                var luisResult = await this.toDoSkillServices.LuisRecognizer.RecognizeAsync<ToDo>(sc.Context, cancellationToken);
                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == ToDo.Intent.ConfirmYes)
                {
                    return await sc.BeginDialogAsync(AddToDoTaskDialog.Name);
                }
                else if (topIntent == ToDo.Intent.ConfirmNo)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.AnythingElseCanDo));
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.AddFirstTask);
                }
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Add first task step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AddFirstTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.AddFirstTask);
        }

        /// <summary>
        /// Ask To Do task content sub step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
            if (!string.IsNullOrEmpty(state.ToDoTaskContent))
            {
                return await sc.NextAsync();
            }
            else
            {
                var prompt = sc.Context.Activity.CreateReply(ToDoBotResponses.AskToDoContentText);
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
        }

        /// <summary>
        /// Check if To Do task content is valid.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AfterAskToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.ToDoTaskContent))
                {
                    if (sc.Result != null)
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out JToken toDoContent);
                        state.ToDoTaskContent = toDoContent != null ? toDoContent.ToString() : sc.Context.Activity.Text;
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
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Collect To Do task content step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> CollectToDoTaskContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectToDoTaskContent);
        }

        /// <summary>
        /// Add To Do task step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AddToDoTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.OneNotePageId))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.SettingUpOneNoteMessage));
                }

                var service = await this.toDoService.Init(state.MsGraphToken, state.OneNotePageId);
                var page = await service.GetDefaultToDoPage();
                state.OneNotePageId = page.Id;
                await service.AddToDoToOneNote(state.ToDoTaskContent, page.ContentUrl);
                var todosAndPageIdTuple = await service.GetMyToDoList();
                state.OneNotePageId = todosAndPageIdTuple.Item2;
                state.ToDoTaskAllActivities = todosAndPageIdTuple.Item1;
                state.ShowToDoPageIndex = 0;
                var rangeCount = Math.Min(state.PageSize, state.ToDoTaskAllActivities.Count);
                state.ToDoTaskActivities = state.ToDoTaskAllActivities.GetRange(0, rangeCount);
                var toDoListAttachment = ToDoHelper.ToAdaptiveCardAttachmentForOtherFlows(
                    state.ToDoTaskActivities,
                    state.ToDoTaskAllActivities.Count,
                    state.ToDoTaskContent,
                    ToDoBotResponses.AfterToDoTaskAdded,
                    ToDoBotResponses.ShowToDoTasks);

                var toDoListReply = sc.Context.Activity.CreateReply();
                toDoListReply.Attachments.Add(toDoListAttachment);
                await sc.Context.SendActivityAsync(toDoListReply);
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Ask To Do task index sub step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
            if (!string.IsNullOrEmpty(state.ToDoTaskContent)
                || state.MarkOrDeleteAllTasksFlag
                || (state.ToDoTaskIndexes.Count == 1
                    && state.ToDoTaskIndexes[0] >= 0
                    && state.ToDoTaskIndexes[0] < state.ToDoTaskActivities.Count))
            {
                return await sc.NextAsync();
            }
            else
            {
                var prompt = sc.Context.Activity.CreateReply(ToDoBotResponses.AskToDoTaskIndex);
                return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
            }
        }

        /// <summary>
        /// Check if To Do task index is valid.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AfterAskToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
            if (string.IsNullOrEmpty(state.ToDoTaskContent)
                && !state.MarkOrDeleteAllTasksFlag
                && (state.ToDoTaskIndexes.Count == 0
                    || state.ToDoTaskIndexes[0] < 0
                    || state.ToDoTaskIndexes[0] >= state.ToDoTaskActivities.Count))
            {
                var luisResult = await this.toDoSkillServices.LuisRecognizer.RecognizeAsync<ToDo>(sc.Context, cancellationToken);
                ToDoHelper.DigestLuisResult(luisResult, sc.Context.Activity.Text, ref state);
            }

            var matchedIndexes = Enumerable.Range(0, state.ToDoTaskAllActivities.Count)
                .Where(i => state.ToDoTaskAllActivities[i].Topic.Equals(state.ToDoTaskContent, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matchedIndexes?.Count > 0)
            {
                state.ToDoTaskIndexes = matchedIndexes;
                return await sc.EndDialogAsync(true);
            }
            else
            {
                var userInput = sc.Context.Activity.Text;
                matchedIndexes = Enumerable.Range(0, state.ToDoTaskAllActivities.Count)
                    .Where(i => state.ToDoTaskAllActivities[i].Topic.Equals(userInput, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchedIndexes?.Count > 0)
                {
                    state.ToDoTaskIndexes = matchedIndexes;
                    return await sc.EndDialogAsync(true);
                }
            }

            if (state.MarkOrDeleteAllTasksFlag)
            {
                return await sc.EndDialogAsync(true);
            }

            if (state.ToDoTaskIndexes.Count == 1
                && state.ToDoTaskIndexes[0] >= 0
                && state.ToDoTaskIndexes[0] < state.ToDoTaskActivities.Count)
            {
                state.ToDoTaskIndexes[0] = (state.PageSize * state.ShowToDoPageIndex) + state.ToDoTaskIndexes[0];
                return await sc.EndDialogAsync(true);
            }
            else
            {
                state.ToDoTaskContent = null;
                return await sc.BeginDialogAsync(Action.CollectToDoTaskIndex);
            }
        }

        /// <summary>
        /// Collect To Do task index step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> CollectToDoTaskIndex(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectToDoTaskIndex);
        }

        /// <summary>
        /// Ask if confirm to delete the To Do task sub step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    var prompt = sc.Context.Activity.CreateReply(ToDoBotResponses.AskDeletionAllConfirmation);
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
                else
                {
                    var toDoTask = state.ToDoTaskAllActivities[state.ToDoTaskIndexes[0]].Topic;
                    var token = new StringDictionary() { { "toDoTask", toDoTask } };
                    var response = ToDoHelper.GenerateResponseWithTokens(ToDoBotResponses.AskDeletionConfirmation, token);
                    var prompt = sc.Context.Activity.CreateReply(response);
                    prompt.Speak = response;
                    return await sc.PromptAsync(Action.Prompt, new PromptOptions() { Prompt = prompt });
                }
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Check if deletion confirmation is valid.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> AfterAskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                var luisResult = await this.toDoSkillServices.LuisRecognizer.RecognizeAsync<ToDo>(sc.Context, cancellationToken);
                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == ToDo.Intent.ConfirmYes)
                {
                    state.DeleteTaskConfirmation = true;
                    return await sc.EndDialogAsync(true);
                }
                else if (topIntent == ToDo.Intent.ConfirmNo)
                {
                    state.DeleteTaskConfirmation = false;
                    return await sc.EndDialogAsync(true);
                }
                else
                {
                    return await sc.BeginDialogAsync(Action.CollectDeleteTaskConfirmation);
                }
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Collect deletion confirmation step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> CollectAskDeletionConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Action.CollectDeleteTaskConfirmation);
        }

        /// <summary>
        /// Delete To Do task step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> DeleteToDoTask(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                if (state.DeleteTaskConfirmation)
                {
                    if (string.IsNullOrEmpty(state.OneNotePageId))
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.SettingUpOneNoteMessage));
                    }

                    var service = await this.toDoService.Init(state.MsGraphToken, state.OneNotePageId);
                    var page = await service.GetDefaultToDoPage();
                    string taskTopicToBeDeleted = null;
                    if (state.MarkOrDeleteAllTasksFlag)
                    {
                        await service.DeleteAllToDos(state.ToDoTaskAllActivities, page.ContentUrl);
                        state.ToDoTaskAllActivities = new List<ToDoTaskActivityModel>();
                        state.ToDoTaskActivities = new List<ToDoTaskActivityModel>();
                        state.ShowToDoPageIndex = 0;
                        state.ToDoTaskIndexes = new List<int>();
                    }
                    else
                    {
                        taskTopicToBeDeleted = state.ToDoTaskAllActivities[state.ToDoTaskIndexes[0]].Topic;
                        var tasksToBeDeleted = new List<ToDoTaskActivityModel>();
                        state.ToDoTaskIndexes.ForEach(i => tasksToBeDeleted.Add(state.ToDoTaskAllActivities[i]));
                        await service.DeleteToDos(tasksToBeDeleted, page.ContentUrl);
                        var todosAndPageIdTuple = await service.GetMyToDoList();
                        state.OneNotePageId = todosAndPageIdTuple.Item2;
                        state.ToDoTaskAllActivities = todosAndPageIdTuple.Item1;
                        var allTasksCount = state.ToDoTaskAllActivities.Count;
                        var currentTaskIndex = state.ShowToDoPageIndex * state.PageSize;
                        while (currentTaskIndex >= allTasksCount && currentTaskIndex >= state.PageSize)
                        {
                            currentTaskIndex -= state.PageSize;
                            state.ShowToDoPageIndex--;
                        }

                        state.ToDoTaskActivities = state.ToDoTaskAllActivities.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                    }

                    if (state.MarkOrDeleteAllTasksFlag)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.AfterAllTasksDeleted));
                    }
                    else
                    {
                        if (state.ToDoTaskActivities.Count > 0)
                        {
                            var deletedToDoListAttachment = ToDoHelper.ToAdaptiveCardAttachmentForOtherFlows(
                                state.ToDoTaskActivities,
                                state.ToDoTaskAllActivities.Count,
                                taskTopicToBeDeleted,
                                ToDoBotResponses.AfterTaskDeleted,
                                ToDoBotResponses.ShowToDoTasks);

                            var deletedToDoListReply = sc.Context.Activity.CreateReply();
                            deletedToDoListReply.Attachments.Add(deletedToDoListAttachment);
                            await sc.Context.SendActivityAsync(deletedToDoListReply);
                        }
                        else
                        {
                            var token1 = new StringDictionary() { { "taskContent", taskTopicToBeDeleted } };
                            var response1 = ToDoHelper.GenerateResponseWithTokens(ToDoBotResponses.AfterTaskDeleted, token1);
                            var token2 = new StringDictionary() { { "taskCount", "0" } };
                            var response2 = ToDoHelper.GenerateResponseWithTokens(ToDoBotResponses.ShowToDoTasks, token2);
                            var response = response1 + " " + response2.Remove(response2.Length - 1) + ".";
                            var botResponse = sc.Context.Activity.CreateReply(response);
                            botResponse.Speak = response;
                            await sc.Context.SendActivityAsync(botResponse);
                        }
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.AnythingElseCanDo));
                }

                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Mark To Do task completed step.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        public async Task<DialogTurnResult> MarkToDoTaskCompleted(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await this.accessors.ToDoSkillState.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.OneNotePageId))
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.SettingUpOneNoteMessage));
                }

                var service = await this.toDoService.Init(state.MsGraphToken, state.OneNotePageId);
                var page = await service.GetDefaultToDoPage();
                BotResponse botResponse;
                string taskTopicToBeMarked = null;
                if (state.MarkOrDeleteAllTasksFlag)
                {
                    await service.MarkAllToDoItemsCompleted(state.ToDoTaskAllActivities, page.ContentUrl);
                    botResponse = ToDoBotResponses.AfterAllToDoTasksCompleted;
                }
                else
                {
                    taskTopicToBeMarked = state.ToDoTaskAllActivities[state.ToDoTaskIndexes[0]].Topic;
                    var tasksToBeMarked = new List<ToDoTaskActivityModel>();
                    state.ToDoTaskIndexes.ForEach(i => tasksToBeMarked.Add(state.ToDoTaskAllActivities[i]));
                    await service.MarkToDoItemsCompleted(tasksToBeMarked, page.ContentUrl);
                    botResponse = ToDoBotResponses.AfterToDoTaskCompleted;
                }

                var todosAndPageIdTuple = await service.GetMyToDoList();
                state.OneNotePageId = todosAndPageIdTuple.Item2;
                state.ToDoTaskAllActivities = todosAndPageIdTuple.Item1;
                var allTasksCount = state.ToDoTaskAllActivities.Count;
                var currentTaskIndex = state.ShowToDoPageIndex * state.PageSize;
                state.ToDoTaskActivities = state.ToDoTaskAllActivities.GetRange(currentTaskIndex, Math.Min(state.PageSize, allTasksCount - currentTaskIndex));
                var markToDoAttachment = ToDoHelper.ToAdaptiveCardAttachmentForOtherFlows(
                    state.ToDoTaskActivities,
                    state.ToDoTaskAllActivities.Count,
                    taskTopicToBeMarked,
                    botResponse,
                    ToDoBotResponses.ShowToDoTasks);
                var markToDoReply = sc.Context.Activity.CreateReply();
                markToDoReply.Attachments.Add(markToDoAttachment);
                await sc.Context.SendActivityAsync(markToDoReply);
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        /// <summary>
        /// Say greeting to user.
        /// </summary>
        /// <param name="sc">Waterfall step context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public async Task<DialogTurnResult> GreetingStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ToDoBotResponses.GreetingMessage));
                return await sc.EndDialogAsync(true);
            }
            catch (Exception ex)
            {
                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(ex.Message));
                await this.accessors.ToDoSkillState.SetAsync(sc.Context, new ToDoSkillState());
                return await sc.CancelAllDialogsAsync();
            }
        }

        // Used for skill/event signin scenarios
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

        // Used for local signin scenarios
        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> pc, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
