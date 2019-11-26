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
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<CalendarSkillState> _stateAccessor;

        public MainDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UserState userState,
            ProactiveState proactiveState,
            CreateEventDialog createEventDialog,
            ChangeEventStatusDialog changeEventStatusDialog,
            TimeRemainingDialog timeRemainingDialog,
            ShowEventsDialog summaryDialog,
            UpdateEventDialog updateEventDialog,
            JoinEventDialog connectToMeetingDialog,
            UpcomingEventDialog upcomingEventDialog,
            CheckAvailableDialog checkAvailableDialog,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog), telemetryClient)
        {
            _settings = settings;
            _services = services;
            _userState = userState;
            _responseManager = responseManager;
            _conversationState = conversationState;
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
            AddDialog(checkAvailableDialog ?? throw new ArgumentNullException(nameof(checkAvailableDialog)));
        }

        protected override async Task OnMembersAddedAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // send a greeting if we're in local mode
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.CalendarWelcomeMessage));
        }

        protected override async Task OnMessageActivityAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());

            // get current activity locale
            var localeConfig = _services.GetCognitiveModels();

            await PopulateStateFromSemanticAction(dc.Context);

            // Initialize the PageSize parameters in state from configuration
            InitializeConfig(state);

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("Calendar", out var luisService);

            var options = new CalendarSkillDialogOptions()
            {
                SubFlowMode = false
            };

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var luisResult = dc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var generalLuisResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var intent = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case CalendarLuis.Intent.FindMeetingRoom:
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
                            await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog), new ChangeEventStatusDialogOptions(options, EventStatus.Cancelled));
                            break;
                        }

                    case CalendarLuis.Intent.ChangeCalendarEntry:
                        {
                            await dc.BeginDialogAsync(nameof(UpdateEventDialog), options);
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
                            await dc.BeginDialogAsync(nameof(CheckAvailableDialog));
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
                                await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage));
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.FeatureNotAvailable));
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

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["Calendar"].RecognizeAsync<CalendarLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.CalendarLuisResultKey, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<General>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
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
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.CancelMessage));
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.End;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.HelpMessage));
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

            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.LogOut));

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