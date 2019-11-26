﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills.Models;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using ToDoSkill.Models;
using ToDoSkill.Responses.Main;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<ToDoSkillState> _toDoStateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            AddToDoItemDialog addToDoItemDialog,
            MarkToDoItemDialog markToDoItemDialog,
            DeleteToDoItemDialog deleteToDoItemDialog,
            ShowToDoItemDialog showToDoItemDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            TelemetryClient = telemetryClient;
            _toDoStateAccessor = conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));

            // RegisterDialogs
            AddDialog(addToDoItemDialog ?? throw new ArgumentNullException(nameof(addToDoItemDialog)));
            AddDialog(markToDoItemDialog ?? throw new ArgumentNullException(nameof(markToDoItemDialog)));
            AddDialog(deleteToDoItemDialog ?? throw new ArgumentNullException(nameof(deleteToDoItemDialog)));
            AddDialog(showToDoItemDialog ?? throw new ArgumentNullException(nameof(showToDoItemDialog)));
        }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.ToDoWelcomeMessage));
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());

            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            // Initialize the PageSize and ReadSize parameters in state from configuration
            InitializeConfig(state);

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("ToDo", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var intent = state.LuisResult?.TopIntent().intent;
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case ToDoLuis.Intent.AddToDo:
                        {
                            await dc.BeginDialogAsync(nameof(AddToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.MarkToDo:
                        {
                            await dc.BeginDialogAsync(nameof(MarkToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.DeleteToDo:
                        {
                            await dc.BeginDialogAsync(nameof(DeleteToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.ShowNextPage:
                    case ToDoLuis.Intent.ShowPreviousPage:
                    case ToDoLuis.Intent.ShowToDo:
                        {
                            await dc.BeginDialogAsync(nameof(ShowToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.ShowNext
                                || generalTopIntent == General.Intent.ShowPrevious)
                            {
                                await dc.BeginDialogAsync(nameof(ShowToDoItemDialog));
                            }
                            else
                            {
                                // No intent was identified, send confused message
                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.DidntUnderstandMessage));
                            }

                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
        }

        protected override async Task OnDialogCompleteAsync(DialogContext dc, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (dc.Context.Adapter is IRemoteUserTokenProvider remoteInvocationAdapter || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.Handoff;

                await dc.Context.SendActivityAsync(response);
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case TokenEvents.TokenResponseEventName:
                    {
                        // Auth dialog completion
                        var result = await dc.ContinueDialogAsync();

                        // If the dialog completed when we sent the token, end the skill conversation
                        if (result.Status != DialogTurnStatus.Waiting)
                        {
                            var response = dc.Context.Activity.CreateReply();
                            response.Type = ActivityTypes.Handoff;

                            await dc.Context.SendActivityAsync(response);
                        }

                        break;
                    }
            }
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // get current activity locale
                var localeConfig = _services.GetCognitiveModels();

                // Update state with email luis result and entities
                var toDoLuisResult = await localeConfig.LuisServices["ToDo"].RecognizeAsync<ToDoLuis>(dc.Context, cancellationToken);
                var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
                state.LuisResult = toDoLuisResult;

                // check luis intent
                localeConfig.LuisServices.TryGetValue("General", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    state.GeneralLuisResult = luisResult;
                    var topIntent = luisResult.TopIntent();

                    if (topIntent.score > 0.5)
                    {
                        switch (topIntent.intent)
                        {
                            case General.Intent.Cancel:
                                {
                                    result = await OnCancel(dc);
                                    break;
                                }

                            case General.Intent.Help:
                                {
                                    // result = await OnHelp(dc);
                                    break;
                                }

                            case General.Intent.Logout:
                                {
                                    result = await OnLogout(dc);
                                    break;
                                }
                        }
                    }
                }
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.CancelMessage));
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.HelpMessage));
            return InterruptionAction.Resume;
        }

        private async Task<InterruptionAction> OnLogout(DialogContext dc)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            await dc.CancelAllDialogsAsync();

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName);
            }

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.LogOut));

            return InterruptionAction.End;
        }

        private void InitializeConfig(ToDoSkillState state)
        {
            // Initialize PageSize and TaskServiceType when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 ? ToDoCommonUtil.DefaultDisplaySize : pageSize;
            }

            if (state.TaskServiceType == ServiceProviderType.Other)
            {
                state.TaskServiceType = ServiceProviderType.Outlook;
                if (!string.IsNullOrEmpty(_settings.TaskServiceProvider))
                {
                    if (_settings.TaskServiceProvider.Equals(ServiceProviderType.OneNote.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.TaskServiceType = ServiceProviderType.OneNote;
                    }
                }
            }
        }
    }
}