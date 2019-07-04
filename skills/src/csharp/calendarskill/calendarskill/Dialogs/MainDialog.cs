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
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
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

            var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            var localeConfig = _services.CognitiveModelSets[locale];
            localeConfig.LuisServices.TryGetValue("calendar", out var luisService);

            var skillOptions = new CalendarSkillDialogOptions
            {
                SubFlowMode = false
            };

            var rootDialog = new AdaptiveDialog(nameof(AdaptiveDialog))
            {
                // Create a LUIS recognizer.
                // The recognizer is built using the intents, utterances, patterns and entities defined in ./RootDialog.lu file
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    // Intent rules for the LUIS model. Each intent here corresponds to an intent defined in ./Dialogs/Resources/ToDoBot.lu file
                    new IntentRule("CreateCalendarEntry")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(CreateEventDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.CreateCalendarEntry.score > 0.4"
                    },
                    new IntentRule("FindMeetingRoom")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(CreateEventDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.FindMeetingRoom.score > 0.4"
                    },
                    new IntentRule("AcceptEventEntry")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ChangeEventStatusDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.AcceptEventEntry.score > 0.4"
                    },
                    new IntentRule("DeleteCalendarEntry")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ChangeEventStatusDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.DeleteCalendarEntry.score > 0.4"
                    },
                    new IntentRule("ChangeCalendarEntry")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(UpdateEventDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.ChangeCalendarEntry.score > 0.4"
                    },
                    new IntentRule("ConnectToMeeting")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(ConnectToMeetingDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.ConnectToMeeting.score > 0.4"
                    },
                    new IntentRule("FindCalendarEntry")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.FindCalendarEntry.score > 0.4"
                    },
                    new IntentRule("FindCalendarWhen")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.FindCalendarWhen.score > 0.4"
                    },
                    new IntentRule("FindCalendarDetail")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.FindCalendarDetail.score > 0.4"
                    },
                    new IntentRule("FindCalendarWhere")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.FindCalendarWhere.score > 0.4"
                    },
                    new IntentRule("FindCalendarWho")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.FindCalendarWho.score > 0.4"
                    },
                    new IntentRule("FindDuration")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.FindDuration.score > 0.4"
                    },
                    new IntentRule("ShowNextCalendar")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.ShowNextCalendar.score > 0.4"
                    },
                    new IntentRule("ShowPreviousCalendar")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(SummaryDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.ShowPreviousCalendar.score > 0.4"
                    },
                    new IntentRule("TimeRemaining")
                    {
                        Steps = new List<IDialog>() { new BeginDialog(nameof(TimeRemainingDialog), options: skillOptions) },
                        Constraint = "turn.dialogEvent.value.intents.TimeRemaining.score > 0.4"
                    },
                    new IntentRule("None")
                    {
                        Steps = new List<IDialog>() { new SendActivity("This is none intent") },
                        Constraint = "turn.dialogEvent.value.intents.None.score > 0.4"
                    },
                    //new UnknownIntentRule() { Steps = new List<IDialog>() { new SendActivity("This is unknown intent") } }
                    //new IntentRule("AddToDoDialog")    { Steps = new List<IDialog>() { new BeginDialog(nameof(AddToDoDialog)) } },
                    //new IntentRule("DeleteToDoDialog") { Steps = new List<IDialog>() { new BeginDialog(nameof(DeleteToDoDialog)) } },
                    //new IntentRule("ViewToDoDialog")   { Steps = new List<IDialog>() { new BeginDialog(nameof(ViewToDoDialog)) } },
                    //// Come back with LG template based readback for global help
                    //new IntentRule("Help")             { Steps = new List<IDialog>() { new SendActivity("[Help-Root-Dialog]") } },
                    //new IntentRule("Cancel")           { Steps = new List<IDialog>() {
                    //}
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(rootDialog);

            rootDialog.AddDialog(new List<IDialog>()
            {
                createEventDialog,
                changeEventStatusDialog,
                summaryDialog,
                timeRemainingDialog,
                updateEventDialog,
                upcomingEventDialog,
                connectToMeetingDialog
            });


            //// Register dialogs
            //AddDialog(createEventDialog ?? throw new ArgumentNullException(nameof(createEventDialog)));
            //AddDialog(changeEventStatusDialog ?? throw new ArgumentNullException(nameof(changeEventStatusDialog)));
            //AddDialog(timeRemainingDialog ?? throw new ArgumentNullException(nameof(timeRemainingDialog)));
            ////AddDialog(summaryDialog ?? throw new ArgumentNullException(nameof(summaryDialog)));
            //AddDialog(updateEventDialog ?? throw new ArgumentNullException(nameof(updateEventDialog)));
            //AddDialog(connectToMeetingDialog ?? throw new ArgumentNullException(nameof(connectToMeetingDialog)));
            //AddDialog(upcomingEventDialog ?? throw new ArgumentNullException(nameof(upcomingEventDialog)));

            InitialDialogId = nameof(AdaptiveDialog);
        }

        public static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/807cd523-34cb-4911-b149-cdcb58f661cc?verbose=true&timezoneOffset=-360&subscription-key=80d731206676475bb03d30e3bc2ee07e&q=",//Configuration["LuisAPIHostName"],
                EndpointKey = "80d731206676475bb03d30e3bc2ee07e", //Configuration["LuisAPIKey"],
                ApplicationId = "807cd523-34cb-4911-b149-cdcb58f661cc",// Configuration["LuisAppId"]
            });
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

            await PopulateStateFromSkillContext(dc.Context);

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
                var intent = state.LuisResult?.TopIntent().intent;
                var generalTopIntent = state.GeneralLuisResult?.TopIntent().intent;

                // switch on general intents
                //switch (intent)
                //{
                //case CalendarLuis.Intent.FindMeetingRoom:
                //case CalendarLuis.Intent.CreateCalendarEntry:
                //    {
                //        turnResult = await dc.BeginDialogAsync(nameof(CreateEventDialog));
                //        break;
                //    }

                //case CalendarLuis.Intent.AcceptEventEntry:
                //case CalendarLuis.Intent.DeleteCalendarEntry:
                //    {
                //        turnResult = await dc.BeginDialogAsync(nameof(ChangeEventStatusDialog));
                //        break;
                //    }

                //case CalendarLuis.Intent.ChangeCalendarEntry:
                //    {
                //        turnResult = await dc.BeginDialogAsync(nameof(UpdateEventDialog));
                //        break;
                //    }

                //case CalendarLuis.Intent.ConnectToMeeting:
                //    {
                //        turnResult = await dc.BeginDialogAsync(nameof(ConnectToMeetingDialog));
                //        break;
                //    }

                //case CalendarLuis.Intent.FindCalendarEntry:
                //case CalendarLuis.Intent.FindCalendarDetail:
                //case CalendarLuis.Intent.FindCalendarWhen:
                //case CalendarLuis.Intent.FindCalendarWhere:
                //case CalendarLuis.Intent.FindCalendarWho:
                //case CalendarLuis.Intent.FindDuration:
                //    {
                //        turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                //        break;
                //    }

                //case CalendarLuis.Intent.TimeRemaining:
                //    {
                //        turnResult = await dc.BeginDialogAsync(nameof(TimeRemainingDialog));
                //        break;
                //    }

                //case CalendarLuis.Intent.ShowNextCalendar:
                //case CalendarLuis.Intent.ShowPreviousCalendar:
                //    {
                //        turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                //        break;
                //    }

                //case CalendarLuis.Intent.None:
                //    {
                //        if (generalTopIntent == General.Intent.ShowNext || generalTopIntent == General.Intent.ShowPrevious)
                //        {
                //            turnResult = await dc.BeginDialogAsync(nameof(SummaryDialog));
                //        }
                //        else
                //        {
                //            await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarSharedResponses.DidntUnderstandMessage));
                //            turnResult = new DialogTurnResult(DialogTurnStatus.Complete);
                //        }

                //        break;
                //    }

                //default:
                //    {
                //        await dc.Context.SendActivityAsync(_responseManager.GetResponse(CalendarMainResponses.FeatureNotAvailable));
                //        turnResult = new DialogTurnResult(DialogTurnStatus.Complete);

                //        break;
                //    }

                //}

                //if (turnResult != EndOfTurn)
                //{
                //    await CompleteAsync(dc);
                //}

                var skillOptions = new CalendarSkillDialogOptions
                {
                    SubFlowMode = false
                };

                if (dc.ActiveDialog == null)
                {
                    await dc.BeginDialogAsync(nameof(AdaptiveDialog), skillOptions);
                }
                else
                {
                    var result = await dc.ContinueDialogAsync();
                }
            }
        }

        private async Task PopulateStateFromSkillContext(ITurnContext context)
        {
            // If we have a SkillContext object populated from the SkillMiddleware we can retrieve requests slot (parameter) data
            // and make available in local state as appropriate.
            var accessor = _userState.CreateProperty<SkillContext>(nameof(SkillContext));
            var skillContext = await accessor.GetAsync(context, () => new SkillContext());
            if (skillContext != null)
            {
                if (skillContext.ContainsKey("timezone"))
                {
                    var timezone = skillContext["timezone"];
                    var state = await _stateAccessor.GetAsync(context, () => new CalendarSkillState());
                    var timezoneJson = timezone as Newtonsoft.Json.Linq.JObject;

                    // we have a timezone
                    state.UserInfo.Timezone = timezoneJson.ToObject<TimeZoneInfo>();
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
                var calendarLuisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<CalendarLuis>(dc.Context, cancellationToken);
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
            public const string DeviceStart = "DeviceStart";
        }
    }
}