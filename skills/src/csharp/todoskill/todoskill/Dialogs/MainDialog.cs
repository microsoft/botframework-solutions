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
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Contextual;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using ToDoSkill.Models;
using ToDoSkill.Responses.Main;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<ToDoSkillState> _toDoStateAccessor;
        private UserContextResolver _userContextResolver;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            AddToDoItemDialog addToDoItemDialog,
            MarkToDoItemDialog markToDoItemDialog,
            DeleteToDoItemDialog deleteToDoItemDialog,
            ShowToDoItemDialog showToDoItemDialog,
            IBotTelemetryClient telemetryClient,
            UserContextResolver userContextResolver)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            TelemetryClient = telemetryClient;
            _toDoStateAccessor = conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));
            _userContextResolver = userContextResolver;

            // RegisterDialogs
            AddDialog(addToDoItemDialog ?? throw new ArgumentNullException(nameof(addToDoItemDialog)));
            AddDialog(markToDoItemDialog ?? throw new ArgumentNullException(nameof(markToDoItemDialog)));
            AddDialog(deleteToDoItemDialog ?? throw new ArgumentNullException(nameof(deleteToDoItemDialog)));
            AddDialog(showToDoItemDialog ?? throw new ArgumentNullException(nameof(showToDoItemDialog)));
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _userContextResolver.ShowPreviousQuestion(dc.Context);

            //await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.ToDoWelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            _userContextResolver.SetDialogIndex();

            var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

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
                var turnResult = EndOfTurn;
                var intent = state.LuisResult?.TopIntent().intent;
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case ToDoLuis.Intent.AddToDo:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(AddToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.MarkToDo:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(MarkToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.DeleteToDo:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(DeleteToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.ShowNextPage:
                    case ToDoLuis.Intent.ShowPreviousPage:
                    case ToDoLuis.Intent.ShowToDo:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ShowToDoItemDialog));
                            break;
                        }

                    case ToDoLuis.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.ShowNext
                                || generalTopIntent == General.Intent.ShowPrevious)
                            {
                                turnResult = await dc.BeginDialogAsync(nameof(ShowToDoItemDialog));
                            }
                            else
                            {
                                // No intent was identified, send confused message
                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.DidntUnderstandMessage));
                                turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            }

                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.FeatureNotAvailable));
                            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                            break;
                        }
                }

                if (turnResult != EndOfTurn)
                {
                    await CompleteAsync(dc);
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // workaround. if connect skill directly to teams, the following response does not work.
            if (dc.Context.Adapter is IRemoteUserTokenProvider remoteInvocationAdapter || Channel.GetChannelId(dc.Context) != Channels.Msteams)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
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
                            response.Type = ActivityTypes.EndOfConversation;

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
                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var cognitiveModels = _services.CognitiveModelSets[locale];

                // Update state with email luis result and entities
                var toDoLuisResult = await cognitiveModels.LuisServices["ToDo"].RecognizeAsync<ToDoLuis>(dc.Context, cancellationToken);
                var state = await _toDoStateAccessor.GetAsync(dc.Context, () => new ToDoSkillState());
                state.LuisResult = toDoLuisResult;

                // check luis intent
                cognitiveModels.LuisServices.TryGetValue("General", out var luisService);

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
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(ToDoMainResponses.HelpMessage));
            return InterruptionAction.MessageSentToUser;
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

            return InterruptionAction.StartedDialog;
        }

        private void InitializeConfig(ToDoSkillState state)
        {
            // Initialize PageSize and TaskServiceType when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = 0;

                if (_settings.DisplaySize != null)
                {
                    pageSize = _settings.DisplaySize;
                }

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