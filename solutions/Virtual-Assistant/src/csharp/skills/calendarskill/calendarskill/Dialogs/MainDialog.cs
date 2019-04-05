// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private ProactiveState _proactiveState;
        private IBackgroundTaskQueue _backgroundTaskQueue;
        private IServiceManager _serviceManager;
        private IStatePropertyAccessor<CalendarSkillState> _stateAccessor;
        private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            ProactiveState proactiveState,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue,
            IServiceManager serviceManager)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _responseManager = responseManager;
            _userState = userState;
            _conversationState = conversationState;
            _proactiveState = proactiveState;
            TelemetryClient = telemetryClient;
            _backgroundTaskQueue = backgroundTaskQueue;
            _serviceManager = serviceManager;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));

            // Register dialogs
            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.CalendarWelcomeMessage));
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

            // Initialize the PageSize parameters in state from configuration
            InitializeConfig(state);

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("calendar", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var turnResult = EndOfTurn;
                var result = await luisService.RecognizeAsync<Luis.CalendarLuis>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case Luis.CalendarLuis.Intent.FindMeetingRoom:
                    case Luis.CalendarLuis.Intent.CreateCalendarEntry:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(CreateEventDialog));
                            break;
                        }

                    case Luis.CalendarLuis.Intent.AcceptEventEntry:
                    case Luis.CalendarLuis.Intent.DeleteCalendarEntry:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog));
                            break;
                        }

                    case Luis.CalendarLuis.Intent.ChangeCalendarEntry:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(UpdateEventDialog));
                            break;
                        }

                    case Luis.CalendarLuis.Intent.ConnectToMeeting:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ConnectToMeetingDialog));
                            break;
                        }

                    case Luis.CalendarLuis.Intent.FindCalendarEntry:
                    case Luis.CalendarLuis.Intent.FindCalendarDetail:
                    case Luis.CalendarLuis.Intent.FindCalendarWhen:
                    case Luis.CalendarLuis.Intent.FindCalendarWhere:
                    case Luis.CalendarLuis.Intent.FindCalendarWho:
                    case Luis.CalendarLuis.Intent.FindDuration:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                            break;
                        }

                    case Luis.CalendarLuis.Intent.TimeRemaining:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(TimeRemainingDialog));
                            break;
                        }

                    case Luis.CalendarLuis.Intent.ShowNextCalendar:
                    case Luis.CalendarLuis.Intent.ShowPreviousCalendar:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                            break;
                        }

                    case Luis.CalendarLuis.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.ShowNext || generalTopIntent == General.Intent.ShowPrevious)
                            {
                                turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                            }
                            else
                            {
                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage));
                                turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.FeatureNotAvailable));
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
            var response = dc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.EndOfConversation;

            await dc.Context.SendActivityAsync(response);

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (dc.Context.Activity.Name)
            {
                case Events.SkillBeginEvent:
                    {
                        var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());

                        if (dc.Context.Activity.Value is Dictionary<string, object> userData)
                        {
                            if (userData.TryGetValue("IPA.Timezone", out var timezone))
                            {
                                // we have a timezone
                                state.UserInfo.Timezone = (TimeZoneInfo)timezone;
                            }
                        }

                        break;
                    }

                case Events.TokenResponseEvent:
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

                case Events.DeviceStart:
                    {
                        await dc.BeginDialogAsync(nameof(UpcomingEventDialog));
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
                var localeConfig = _services.CognitiveModelSets[locale];

                // Update state with email luis result and entities
                var calendarLuisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<Luis.CalendarLuis>(dc.Context, cancellationToken);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());
                state.LuisResult = calendarLuisResult;

                // check luis intent
                localeConfig.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    state.GeneralLuisResult = luisResult;
                    var topIntent = luisResult.TopIntent().intent;

                    // check intent
                    switch (topIntent)
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

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());
            state.Clear();
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.CancelMessage));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.HelpMessage));
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

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new CreateEventDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new ChangeEventStatusDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new TimeRemainingDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new SummaryDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new UpdateEventDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new ConnectToMeetingDialog(_settings, _services, _responseManager, _stateAccessor, _serviceManager, TelemetryClient));
            AddDialog(new UpcomingEventDialog(_settings, _services, _responseManager, _stateAccessor, _proactiveStateAccessor, _serviceManager, TelemetryClient, _backgroundTaskQueue));
        }

        private void InitializeConfig(CalendarSkillState state)
        {
            // Initialize PageSize when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = 0;
                if (_settings.Properties.TryGetValue("DisplaySize", out var displaySizeObj))
                {
                    int.TryParse(displaySizeObj.ToString(), out pageSize);
                }

                state.PageSize = pageSize <= 0 || pageSize > CalendarCommonUtil.MaxDisplaySize ? CalendarCommonUtil.MaxDisplaySize : pageSize;
            }
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
            public const string DeviceStart = "DeviceStart";
        }
    }
}