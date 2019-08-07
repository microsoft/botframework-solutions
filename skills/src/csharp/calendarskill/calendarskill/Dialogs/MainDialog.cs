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
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using static CalendarSkill.Models.EventModel;

namespace CalendarSkill.Dialogs
{
    public class MainDialog : RouterDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<CalendarSkillState> _stateAccessor;

        //private TemplateEngine _lgEngine;
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

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
            SummaryDialog summaryDialog,
            UpdateEventDialog updateEventDialog,
            ConnectToMeetingDialog connectToMeetingDialog,
            UpcomingEventDialog upcomingEventDialog,
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

            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("MainDialog.lg");

            // Register dialogs
            AddDialog(createEventDialog ?? throw new ArgumentNullException(nameof(createEventDialog)));
            AddDialog(changeEventStatusDialog ?? throw new ArgumentNullException(nameof(changeEventStatusDialog)));
            AddDialog(timeRemainingDialog ?? throw new ArgumentNullException(nameof(timeRemainingDialog)));
            AddDialog(summaryDialog ?? throw new ArgumentNullException(nameof(summaryDialog)));
            AddDialog(updateEventDialog ?? throw new ArgumentNullException(nameof(updateEventDialog)));
            AddDialog(connectToMeetingDialog ?? throw new ArgumentNullException(nameof(connectToMeetingDialog)));
            AddDialog(upcomingEventDialog ?? throw new ArgumentNullException(nameof(upcomingEventDialog)));
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());
            // send a greeting if we're in local mode
            var activity = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, dc.Context, "[CalendarWelcomeMessage]", null);

            await dc.Context.SendActivityAsync(activity);
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());

            // get current activity locale
            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];

            await PopulateStateFromSemanticAction(dc.Context);

            // Initialize the PageSize parameters in state from configuration
            InitializeConfig(state);

            // If dispatch result is general luis model
            localeConfig.LuisServices.TryGetValue("Calendar", out var luisService);

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
                    case CalendarLuis.Intent.FindMeetingRoom:
                    case CalendarLuis.Intent.CreateCalendarEntry:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(CreateEventDialog));
                            break;
                        }

                    case CalendarLuis.Intent.AcceptEventEntry:
                    case CalendarLuis.Intent.DeleteCalendarEntry:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog));
                            break;
                        }

                    case CalendarLuis.Intent.ChangeCalendarEntry:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(UpdateEventDialog));
                            break;
                        }

                    case CalendarLuis.Intent.ConnectToMeeting:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(ConnectToMeetingDialog));
                            break;
                        }

                    case CalendarLuis.Intent.FindCalendarEntry:
                    case CalendarLuis.Intent.FindCalendarDetail:
                    case CalendarLuis.Intent.FindCalendarWhen:
                    case CalendarLuis.Intent.FindCalendarWhere:
                    case CalendarLuis.Intent.FindCalendarWho:
                    case CalendarLuis.Intent.FindDuration:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                            break;
                        }

                    case CalendarLuis.Intent.TimeRemaining:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(TimeRemainingDialog));
                            break;
                        }

                    case CalendarLuis.Intent.ShowNextCalendar:
                    case CalendarLuis.Intent.ShowPreviousCalendar:
                        {
                            turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                            break;
                        }

                    case CalendarLuis.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.ShowNext || generalTopIntent == General.Intent.ShowPrevious)
                            {
                                turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                            }
                            else
                            {
                                await dc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, dc.Context, "[DidntUnderstandMessage]", null));
                                turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, dc.Context, "[FeatureNotAvailable]", null));
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

        private async Task PopulateStateFromSemanticAction(ITurnContext context)
        {
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;
            if (semanticAction != null && semanticAction.Entities.ContainsKey("timezone"))
            {
                var timezone = semanticAction.Entities["timezone"];
                var timezoneObj = timezone.Properties["timezone"].ToObject<TimeZoneInfo>();

                var state = await _stateAccessor.GetAsync(context, () => new CalendarSkillState());

                // we have a timezone
                state.UserInfo.Timezone = timezoneObj;
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
                case SkillEvents.FallbackHandledEventName:
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
                var calendarLuisResult = await localeConfig.LuisServices["Calendar"].RecognizeAsync<CalendarLuis>(dc.Context, cancellationToken);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());
                state.LuisResult = calendarLuisResult;

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
            await dc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, dc.Context, "[CancelMessage]", null));
            await CompleteAsync(dc);
            await dc.CancelAllDialogsAsync();
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, dc.Context, "[HelpMessage]", null));
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

            await dc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, dc.Context, "[LogOut]", null));

            return InterruptionAction.StartedDialog;
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