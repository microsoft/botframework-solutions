using System;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using SkillServiceLibrary.Utilities;

namespace CalendarSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private LocaleTemplateEngineManager _templateEngine;
        private IStatePropertyAccessor<CalendarSkillState> _stateAccessor;
        private Dialog _createEventDialog;
        private Dialog _changeEventStatusDialog;
        private Dialog _timeRemainingDialog;
        private Dialog _showEventsDialog;
        private Dialog _updateEventDialog;
        private Dialog _joinEventDialog;
        private Dialog _upcomingEventDialog;
        private Dialog _checkPersonAvailableDialog;
        private Dialog _findMeetingRoomDialog;
        private Dialog _updateMeetingRoomDialog;
        private Dialog _bookMeetingRoomDialog;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateEngine = serviceProvider.GetService<LocaleTemplateEngineManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            _createEventDialog = serviceProvider.GetService<CreateEventDialog>() ?? throw new ArgumentNullException(nameof(CreateEventDialog));
            _changeEventStatusDialog = serviceProvider.GetService<ChangeEventStatusDialog>() ?? throw new ArgumentNullException(nameof(ChangeEventStatusDialog));
            _timeRemainingDialog = serviceProvider.GetService<TimeRemainingDialog>() ?? throw new ArgumentNullException(nameof(TimeRemainingDialog));
            _showEventsDialog = serviceProvider.GetService<ShowEventsDialog>() ?? throw new ArgumentNullException(nameof(ShowEventsDialog));
            _updateEventDialog = serviceProvider.GetService<UpdateEventDialog>() ?? throw new ArgumentNullException(nameof(UpdateEventDialog));
            _joinEventDialog = serviceProvider.GetService<JoinEventDialog>() ?? throw new ArgumentNullException(nameof(JoinEventDialog));
            _upcomingEventDialog = serviceProvider.GetService<UpcomingEventDialog>() ?? throw new ArgumentNullException(nameof(UpcomingEventDialog));
            _checkPersonAvailableDialog = serviceProvider.GetService<CheckPersonAvailableDialog>() ?? throw new ArgumentNullException(nameof(CheckPersonAvailableDialog));
            _findMeetingRoomDialog = serviceProvider.GetService<FindMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(FindMeetingRoomDialog));
            _updateMeetingRoomDialog = serviceProvider.GetService<UpdateMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(UpdateMeetingRoomDialog));
            _updateEventDialog = serviceProvider.GetService<UpdateEventDialog>() ?? throw new ArgumentNullException(nameof(UpdateEventDialog));
            _bookMeetingRoomDialog = serviceProvider.GetService<BookMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(BookMeetingRoomDialog));
            AddDialog(_createEventDialog);
            AddDialog(_changeEventStatusDialog);
            AddDialog(_timeRemainingDialog);
            AddDialog(_showEventsDialog);
            AddDialog(_updateEventDialog);
            AddDialog(_joinEventDialog);
            AddDialog(_upcomingEventDialog);
            AddDialog(_checkPersonAvailableDialog);
            AddDialog(_findMeetingRoomDialog);
            AddDialog(_updateMeetingRoomDialog);
            AddDialog(_bookMeetingRoomDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Calendar", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<CalendarLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.CalendarLuisResultKey] = skillResult;
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Calendar", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<CalendarLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.CalendarLuisResultKey] = skillResult;
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected async Task<bool> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var interrupted = false;
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get connected LUIS result from turn state.
                var state = await _stateAccessor.GetAsync(innerDc.Context, () => new CalendarSkillState());
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(CalendarMainResponses.CancelMessage));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(CalendarMainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_templateEngine.GenerateActivityForLocale(CalendarMainResponses.LogOut));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }
                    }

                    if (!interrupted && innerDc.ActiveDialog != null)
                    {
                        var calendarLuisResult = innerDc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                        var topCalendarIntent = calendarLuisResult.TopIntent();

                        if (topCalendarIntent.score > 0.9 && !CalendarCommonUtil.IsFindEventsDialog(state.InitialIntent))
                        {
                            var intentSwitchingResult = CalendarCommonUtil.CheckIntentSwitching(topCalendarIntent.intent);
                            var newFlowOptions = new CalendarSkillDialogOptions() { SubFlowMode = false };

                            if (intentSwitchingResult != CalendarLuis.Intent.None)
                            {
                                state.Clear();
                                await innerDc.CancelAllDialogsAsync();
                                state.InitialIntent = intentSwitchingResult;

                                switch (intentSwitchingResult)
                                {
                                    case CalendarLuis.Intent.DeleteCalendarEntry:
                                        await innerDc.BeginDialogAsync(nameof(ChangeEventStatusDialog), new ChangeEventStatusDialogOptions(newFlowOptions, EventStatus.Cancelled));
                                        interrupted = true;
                                        break;
                                    case CalendarLuis.Intent.AcceptEventEntry:
                                        await innerDc.BeginDialogAsync(nameof(ChangeEventStatusDialog), new ChangeEventStatusDialogOptions(newFlowOptions, EventStatus.Accepted));
                                        interrupted = true;
                                        break;
                                    case CalendarLuis.Intent.ChangeCalendarEntry:
                                        await innerDc.BeginDialogAsync(nameof(UpdateEventDialog), newFlowOptions);
                                        interrupted = true;
                                        break;
                                    case CalendarLuis.Intent.CheckAvailability:
                                        await innerDc.BeginDialogAsync(nameof(CheckPersonAvailableDialog), newFlowOptions);
                                        interrupted = true;
                                        break;
                                    case CalendarLuis.Intent.ConnectToMeeting:
                                        await innerDc.BeginDialogAsync(nameof(JoinEventDialog), newFlowOptions);
                                        interrupted = true;
                                        break;
                                    case CalendarLuis.Intent.CreateCalendarEntry:
                                        await innerDc.BeginDialogAsync(nameof(CreateEventDialog), newFlowOptions);
                                        interrupted = true;
                                        break;
                                    case CalendarLuis.Intent.FindCalendarDetail:
                                    case CalendarLuis.Intent.FindCalendarEntry:
                                    case CalendarLuis.Intent.FindCalendarWhen:
                                    case CalendarLuis.Intent.FindCalendarWhere:
                                    case CalendarLuis.Intent.FindCalendarWho:
                                    case CalendarLuis.Intent.FindDuration:
                                        await innerDc.BeginDialogAsync(nameof(ShowEventsDialog), new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, newFlowOptions));
                                        interrupted = true;
                                        break;
                                    case CalendarLuis.Intent.TimeRemaining:
                                        await innerDc.BeginDialogAsync(nameof(TimeRemainingDialog), newFlowOptions);
                                        interrupted = true;
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return interrupted;
        }

        // Handles introduction/continuation prompt logic.
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // If the bot is in skill mode, skip directly to route and do not prompt
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _templateEngine.GenerateActivityForLocale(CalendarMainResponses.CalendarWelcomeMessage)
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CalendarSkillState());

            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                var luisResult = stepContext.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var intent = luisResult?.TopIntent().intent;
                state.InitialIntent = intent.Value;

                var generalResult = stepContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var generalIntent = generalResult?.TopIntent().intent;

                InitializeConfig(state);

                var options = new CalendarSkillDialogOptions()
                {
                    SubFlowMode = false
                };

                // switch on general intents
                switch (intent)
                {
                    case CalendarLuis.Intent.FindMeetingRoom:
                        {
                            // check whether the meeting room feature supported.
                            if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                            {
                                return await stepContext.BeginDialogAsync(_bookMeetingRoomDialog.Id, options);
                            }
                            else
                            {
                                var activity = _templateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await stepContext.Context.SendActivityAsync(activity);
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
                                    return await stepContext.BeginDialogAsync(_updateMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                var activity = _templateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await stepContext.Context.SendActivityAsync(activity);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.CreateCalendarEntry:
                        {
                            return await stepContext.BeginDialogAsync(_createEventDialog.Id, options);
                        }

                    case CalendarLuis.Intent.AcceptEventEntry:
                        {
                            return await stepContext.BeginDialogAsync(_changeEventStatusDialog.Id, new ChangeEventStatusDialogOptions(options, EventStatus.Accepted));
                        }

                    case CalendarLuis.Intent.DeleteCalendarEntry:
                        {
                            if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                            {
                                if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                                {
                                    return await stepContext.BeginDialogAsync(_updateMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                return await stepContext.BeginDialogAsync(_changeEventStatusDialog.Id, new ChangeEventStatusDialogOptions(options, EventStatus.Cancelled));
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ChangeCalendarEntry:
                        {
                            if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                            {
                                if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                                {
                                    return await stepContext.BeginDialogAsync(_updateMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                return await stepContext.BeginDialogAsync(_updateEventDialog.Id, options);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ConnectToMeeting:
                        {
                            return await stepContext.BeginDialogAsync(_joinEventDialog.Id, options);
                        }

                    case CalendarLuis.Intent.FindCalendarEntry:
                    case CalendarLuis.Intent.FindCalendarDetail:
                    case CalendarLuis.Intent.FindCalendarWhen:
                    case CalendarLuis.Intent.FindCalendarWhere:
                    case CalendarLuis.Intent.FindCalendarWho:
                    case CalendarLuis.Intent.FindDuration:
                        {
                            return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                        }

                    case CalendarLuis.Intent.TimeRemaining:
                        {
                            return await stepContext.BeginDialogAsync(_timeRemainingDialog.Id);
                        }

                    case CalendarLuis.Intent.CheckAvailability:
                        {
                            if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                            {
                                if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                                {
                                    state.InitialIntent = CalendarLuis.Intent.FindMeetingRoom;
                                    return await stepContext.BeginDialogAsync(_bookMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                return await stepContext.BeginDialogAsync(_checkPersonAvailableDialog.Id);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ShowNextCalendar:
                    case CalendarLuis.Intent.ShowPreviousCalendar:
                        {
                            return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                        }

                    case CalendarLuis.Intent.None:
                        {
                            if (generalIntent == General.Intent.ShowNext || generalIntent == General.Intent.ShowPrevious)
                            {
                                return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                            }
                            else
                            {
                                var activity = _templateEngine.GenerateActivityForLocale(CalendarSharedResponses.DidntUnderstandMessage);
                                await stepContext.Context.SendActivityAsync(activity);
                            }

                            break;
                        }

                    default:
                        {
                            var activity = _templateEngine.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                            await stepContext.Context.SendActivityAsync(activity);
                            break;
                        }
                }
            }
            else if (a.Type == ActivityTypes.Event)
            {
                var ev = a.AsEventActivity();

                switch (stepContext.Context.Activity.Name)
                {
                    case Events.DeviceStart:
                        {
                            return await stepContext.BeginDialogAsync(_upcomingEventDialog.Id);
                        }

                    default:
                        {
                            await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                            break;
                        }
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync();
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // EndOfConversation activity should be passed back to indicate that VA should resume control of the conversation
                var endOfConversation = new Activity(ActivityTypes.EndOfConversation)
                {
                    Code = EndOfConversationCodes.CompletedSuccessfully,
                    Value = stepContext.Result,
                };

                await stepContext.Context.SendActivityAsync(endOfConversation, cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(this.Id, _templateEngine.GenerateActivityForLocale(CalendarMainResponses.CalendarWelcomeMessage), cancellationToken);
            }
        }

        private async Task LogUserOut(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
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
