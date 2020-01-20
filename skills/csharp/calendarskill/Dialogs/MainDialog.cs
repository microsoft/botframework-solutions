// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Services.AzureMapsAPI;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills.Models;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs
{
    public class MainDialog : ActivityHandlerDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<CalendarSkillState> _stateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            CreateEventDialog createEventDialog,
            ChangeEventStatusDialog changeEventStatusDialog,
            TimeRemainingDialog timeRemainingDialog,
            ShowEventsDialog summaryDialog,
            UpdateEventDialog updateEventDialog,
            JoinEventDialog connectToMeetingDialog,
            UpcomingEventDialog upcomingEventDialog,
            CheckPersonAvailableDialog checkPersonAvailableDialog,
            FindMeetingRoomDialog findMeetingRoomDialog,
            UpdateMeetingRoomDialog updateMeetingRoomDialog,
            BookMeetingRoomDialog bookMeetingRoomDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _userState = userState;
            _conversationState = conversationState;
            TemplateEngine = localeTemplateEngineManager;
            TelemetryClient = telemetryClient;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));

            // Register dialogs
            AddDialog(createEventDialog ?? throw new ArgumentNullException(nameof(createEventDialog)));
            AddDialog(changeEventStatusDialog ?? throw new ArgumentNullException(nameof(changeEventStatusDialog)));
            AddDialog(timeRemainingDialog ?? throw new ArgumentNullException(nameof(timeRemainingDialog)));
            AddDialog(summaryDialog ?? throw new ArgumentNullException(nameof(summaryDialog)));
            AddDialog(updateEventDialog ?? throw new ArgumentNullException(nameof(updateEventDialog)));
            AddDialog(connectToMeetingDialog ?? throw new ArgumentNullException(nameof(connectToMeetingDialog)));
            AddDialog(upcomingEventDialog ?? throw new ArgumentNullException(nameof(upcomingEventDialog)));
            AddDialog(checkPersonAvailableDialog ?? throw new ArgumentNullException(nameof(checkPersonAvailableDialog)));
            AddDialog(findMeetingRoomDialog ?? throw new ArgumentNullException(nameof(findMeetingRoomDialog)));
            AddDialog(updateMeetingRoomDialog ?? throw new ArgumentNullException(nameof(updateMeetingRoomDialog)));
            AddDialog(bookMeetingRoomDialog ?? throw new ArgumentNullException(nameof(bookMeetingRoomDialog)));
        }

        private LocaleTemplateEngineManager TemplateEngine { get; set; }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.CalendarWelcomeMessage);
            await dc.Context.SendActivityAsync(activity);
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());

            await PopulateStateFromSemanticAction(dc.Context);

            // Initialize the PageSize parameters in state from configuration
            InitializeConfig(state);

            var options = new CalendarSkillDialogOptions()
            {
                SubFlowMode = false
            };

            var luisResult = dc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
            var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
            var intent = luisResult?.TopIntent().intent;
            var generalTopIntent = generalLuisResult?.TopIntent().intent;

            state.InitialIntent = intent.Value;

            // switch on general intents
            switch (intent)
            {
                case CalendarLuis.Intent.FindMeetingRoom:
                    {
                        // check whether the meeting room feature supported.
                        if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                        {
                            await dc.BeginDialogAsync(nameof(BookMeetingRoomDialog), options);
                        }
                        else
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                            await dc.Context.SendActivityAsync(activity);
                        }

                        break;
                    }

                case CalendarLuis.Intent.AddCalendarEntryAttribute:
                    {
                        // Determine the exact intent using entities
                        if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                        {
                            if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                            {
                                await dc.BeginDialogAsync(nameof(UpdateMeetingRoomDialog), options);
                            }
                            else
                            {
                                var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await dc.Context.SendActivityAsync(activity);
                            }
                        }
                        else
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                            await dc.Context.SendActivityAsync(activity);
                        }

                        break;
                    }

                case CalendarLuis.Intent.CreateCalendarEntry:
                    {
                        await dc.BeginDialogAsync(nameof(CreateEventDialog), options);
                        break;
                    }

                case CalendarLuis.Intent.AcceptEventEntry:
                    {
                        await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog), new ChangeEventStatusDialogOptions(options, EventStatus.Accepted));
                        break;
                    }

                case CalendarLuis.Intent.DeleteCalendarEntry:
                    {
                        if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                        {
                            if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                            {
                                await dc.BeginDialogAsync(nameof(UpdateMeetingRoomDialog), options);
                            }
                            else
                            {
                                var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await dc.Context.SendActivityAsync(activity);
                            }
                        }
                        else
                        {
                            await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog), new ChangeEventStatusDialogOptions(options, EventStatus.Cancelled));
                        }

                        break;
                    }

                case CalendarLuis.Intent.ChangeCalendarEntry:
                    {
                        if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                        {
                            if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                            {
                                await dc.BeginDialogAsync(nameof(UpdateMeetingRoomDialog), options);
                            }
                            else
                            {
                                var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await dc.Context.SendActivityAsync(activity);
                            }
                        }
                        else
                        {
                            await dc.BeginDialogAsync(nameof(UpdateEventDialog), options);
                        }

                        break;
                    }

                case CalendarLuis.Intent.ConnectToMeeting:
                    {
                        await dc.BeginDialogAsync(nameof(JoinEventDialog), options);
                        break;
                    }

                case CalendarLuis.Intent.FindCalendarEntry:
                case CalendarLuis.Intent.FindCalendarDetail:
                case CalendarLuis.Intent.FindCalendarWhen:
                case CalendarLuis.Intent.FindCalendarWhere:
                case CalendarLuis.Intent.FindCalendarWho:
                case CalendarLuis.Intent.FindDuration:
                    {
                        await dc.BeginDialogAsync(nameof(ShowEventsDialog), new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                        break;
                    }

                case CalendarLuis.Intent.TimeRemaining:
                    {
                        await dc.BeginDialogAsync(nameof(TimeRemainingDialog));
                        break;
                    }

                case CalendarLuis.Intent.CheckAvailability:
                    {
                        if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                        {
                            if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                            {
                                state.InitialIntent = CalendarLuis.Intent.FindMeetingRoom;
                                await dc.BeginDialogAsync(nameof(BookMeetingRoomDialog), options);
                            }
                            else
                            {
                                var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await dc.Context.SendActivityAsync(activity);
                            }
                        }
                        else
                        {
                            await dc.BeginDialogAsync(nameof(CheckPersonAvailableDialog));
                        }

                        break;
                    }

                case CalendarLuis.Intent.ShowNextCalendar:
                case CalendarLuis.Intent.ShowPreviousCalendar:
                    {
                        await dc.BeginDialogAsync(nameof(ShowEventsDialog), new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                        break;
                    }

                case CalendarLuis.Intent.None:
                    {
                        if (generalTopIntent == General.Intent.ShowNext || generalTopIntent == General.Intent.ShowPrevious)
                        {
                            await dc.BeginDialogAsync(nameof(ShowEventsDialog), new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                        }
                        else
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(CalendarSharedResponses.DidntUnderstandMessage);
                            await dc.Context.SendActivityAsync(activity);
                        }

                        break;
                    }

                default:
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                        await dc.Context.SendActivityAsync(activity);
                        break;
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

            // End active dialog.
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

                case Events.DeviceStart:
                    {
                        await dc.BeginDialogAsync(nameof(UpcomingEventDialog));
                        break;
                    }
            }
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();
                var skillResult = innerDc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                if (skillResult == null)
                {
                    // Run LUIS recognition on Skill model and store result in turn state.
                    localizedServices.LuisServices.TryGetValue("Calendar", out var skillLuisService);
                    if (skillLuisService != null)
                    {
                        skillResult = await skillLuisService.RecognizeAsync<CalendarLuis>(innerDc.Context, cancellationToken);
                        innerDc.Context.TurnState.Add(StateProperties.CalendarLuisResultKey, skillResult);
                    }
                    else
                    {
                        throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                    }
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // get current activity locale
                var localeConfig = _services.GetCognitiveModels();

                var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());
                var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var topIntent = generalLuisResult.TopIntent();

                if (topIntent.score > 0.5)
                {
                    // check intent
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

                    if (result == InterruptionAction.NoAction && dc.ActiveDialog != null)
                    {
                        var calendarLuisResult = dc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                        var topCalendarIntent = calendarLuisResult.TopIntent();

                        if (topCalendarIntent.score > 0.9 && !CalendarCommonUtil.IsFindEventsDialog(state.InitialIntent))
                        {
                            var intentSwitchingResult = CalendarCommonUtil.CheckIntentSwitching(topCalendarIntent.intent);
                            var newFlowOptions = new CalendarSkillDialogOptions() { SubFlowMode = false };

                            if (intentSwitchingResult != CalendarLuis.Intent.None)
                            {
                                result = InterruptionAction.Waiting;
                                state.Clear();
                                await dc.CancelAllDialogsAsync();
                                state.InitialIntent = intentSwitchingResult;

                                switch (intentSwitchingResult)
                                {
                                    case CalendarLuis.Intent.DeleteCalendarEntry:
                                        await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog), new ChangeEventStatusDialogOptions(newFlowOptions, EventStatus.Cancelled));
                                        break;
                                    case CalendarLuis.Intent.AcceptEventEntry:
                                        await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog), new ChangeEventStatusDialogOptions(newFlowOptions, EventStatus.Accepted));
                                        break;
                                    case CalendarLuis.Intent.ChangeCalendarEntry:
                                        await dc.BeginDialogAsync(nameof(UpdateEventDialog), newFlowOptions);
                                        break;
                                    case CalendarLuis.Intent.CheckAvailability:
                                        await dc.BeginDialogAsync(nameof(CheckPersonAvailableDialog), newFlowOptions);
                                        break;
                                    case CalendarLuis.Intent.ConnectToMeeting:
                                        await dc.BeginDialogAsync(nameof(JoinEventDialog), newFlowOptions);
                                        break;
                                    case CalendarLuis.Intent.CreateCalendarEntry:
                                        await dc.BeginDialogAsync(nameof(CreateEventDialog), newFlowOptions);
                                        break;
                                    case CalendarLuis.Intent.FindCalendarDetail:
                                    case CalendarLuis.Intent.FindCalendarEntry:
                                    case CalendarLuis.Intent.FindCalendarWhen:
                                    case CalendarLuis.Intent.FindCalendarWhere:
                                    case CalendarLuis.Intent.FindCalendarWho:
                                    case CalendarLuis.Intent.FindDuration:
                                        await dc.BeginDialogAsync(nameof(ShowEventsDialog), new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, newFlowOptions));
                                        break;
                                    case CalendarLuis.Intent.TimeRemaining:
                                        await dc.BeginDialogAsync(nameof(TimeRemainingDialog), newFlowOptions);
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private async Task PopulateStateFromSemanticAction(ITurnContext context)
        {
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;
            if (semanticAction != null && semanticAction.Entities.ContainsKey(StateProperties.TimezoneKey))
            {
                var timezone = semanticAction.Entities[StateProperties.TimezoneKey];
                var timezoneObj = timezone.Properties[StateProperties.TimezoneKey].ToObject<TimeZoneInfo>();

                var state = await _stateAccessor.GetAsync(context, () => new CalendarSkillState());

                // we have a timezone
                state.UserInfo.Timezone = timezoneObj;
            }

            if (semanticAction != null && semanticAction.Entities.ContainsKey(StateProperties.LocationKey))
            {
                var location = semanticAction.Entities[StateProperties.LocationKey];
                var locationString = location.Properties[StateProperties.LocationKey].ToString();
                var state = await _stateAccessor.GetAsync(context, () => new CalendarSkillState());

                var coords = locationString.Split(',');
                if (coords.Length == 2)
                {
                    if (double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                    {
                        state.UserInfo.Latitude = lat;
                        state.UserInfo.Longitude = lng;
                    }
                }

                var azureMapsClient = new AzureMapsClient(_settings);
                var timezone = await azureMapsClient.GetTimeZoneInfoByCoordinates(locationString);

                state.UserInfo.Timezone = timezone;
            }
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());
            state.Clear();

            var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.CancelMessage);
            await dc.Context.SendActivityAsync(activity);

            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.HelpMessage);
            await dc.Context.SendActivityAsync(activity);

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

            var activity = TemplateEngine.GenerateActivityForLocale(CalendarMainResponses.LogOut);
            await dc.Context.SendActivityAsync(activity);

            return InterruptionAction.End;
        }

        private void InitializeConfig(CalendarSkillState state)
        {
            // Initialize PageSize when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 || pageSize > CalendarCommonUtil.MaxDisplaySize ? CalendarCommonUtil.MaxDisplaySize : pageSize;
            }
        }

        private class Events
        {
            public const string DeviceStart = "DeviceStart";
        }
    }
}