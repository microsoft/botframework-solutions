// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill
{
    public class MainDialog : RouterDialog
    {
        private bool _skillMode;
        private SkillConfiguration _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private IServiceManager _serviceManager;
        private IStatePropertyAccessor<CalendarSkillState> _stateAccessor;
        private CalendarSkillResponseBuilder _responseBuilder = new CalendarSkillResponseBuilder();

        public MainDialog(
            SkillConfiguration services,
            ConversationState conversationState,
            UserState userState,
            IServiceManager serviceManager,
            bool skillMode)
            : base(nameof(MainDialog))
        {
            _skillMode = skillMode;
            _services = services;
            _userState = userState;
            _conversationState = conversationState;
            _serviceManager = serviceManager;

            // Initialize state accessor
            _stateAccessor = _conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));

            // Register dialogs
            RegisterDialogs();
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!_skillMode)
            {
                // send a greeting if we're in local mode
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarMainResponses.CalendarWelcomeMessage));
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());

            // If dispatch result is general luis model
            _services.LuisServices.TryGetValue("calendar", out var luisService);

            if (luisService == null)
            {
                throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
            }
            else
            {
                var result = await luisService.RecognizeAsync<Calendar>(dc.Context, CancellationToken.None);
                var intent = result?.TopIntent().intent;

                var skillOptions = new CalendarSkillDialogOptions
                {
                    SkillMode = _skillMode,
                };

                // switch on general intents
                switch (intent)
                {
                    case Calendar.Intent.FindMeetingRoom:
                    case Calendar.Intent.CreateCalendarEntry:
                        {
                            await dc.BeginDialogAsync(nameof(CreateEventDialog), skillOptions);
                            break;
                        }

                    case Calendar.Intent.DeleteCalendarEntry:
                        {
                            await dc.BeginDialogAsync(nameof(DeleteEventDialog), skillOptions);
                            break;
                        }

                    case Calendar.Intent.NextMeeting:
                        {
                            await dc.BeginDialogAsync(nameof(NextMeetingDialog), skillOptions);
                            break;
                        }

                    case Calendar.Intent.ChangeCalendarEntry:
                        {
                            await dc.BeginDialogAsync(nameof(UpdateEventDialog), skillOptions);
                            break;
                        }

                    case Calendar.Intent.FindCalendarEntry:
                    case Calendar.Intent.Summary:
                        {
                            await dc.BeginDialogAsync(nameof(SummaryDialog), skillOptions);
                            break;
                        }

                    case Calendar.Intent.None:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarSharedResponses.DidntUnderstandMessage));
                            if (_skillMode)
                            {
                                await CompleteAsync(dc);
                            }

                            break;
                        }

                    default:
                        {
                            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarMainResponses.FeatureNotAvailable));

                            if (_skillMode)
                            {
                                await CompleteAsync(dc);
                            }

                            break;
                        }
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_skillMode)
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);
            }
            else
            {
                await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarSharedResponses.ActionEnded));
            }

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

                        state.EventSource = EventSource.Microsoft;
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
            }
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = InterruptionAction.NoAction;

            if (dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Update state with email luis result and entities
                var calendarLuisResult = await _services.LuisServices["calendar"].RecognizeAsync<Calendar>(dc.Context, cancellationToken);
                var state = await _stateAccessor.GetAsync(dc.Context, () => new CalendarSkillState());
                state.LuisResult = calendarLuisResult;

                // check luis intent
                _services.LuisServices.TryGetValue("general", out var luisService);

                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Skill configuration.");
                }
                else
                {
                    var luisResult = await luisService.RecognizeAsync<General>(dc.Context, cancellationToken);
                    state.GeneralLuisResult = luisResult;
                    var topIntent = luisResult.TopIntent().intent;

                    if (luisResult.TopIntent().score > 0.5)
                    {
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
            }

            return result;
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            await dc.BeginDialogAsync(nameof(CancelDialog));
            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarMainResponses.HelpMessage));
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
            await adapter.SignOutUserAsync(dc.Context, _services.AuthConnectionName);
            await dc.Context.SendActivityAsync(dc.Context.Activity.CreateReply(CalendarMainResponses.LogOut));

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new CreateEventDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new DeleteEventDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new NextMeetingDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new SummaryDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new UpdateEventDialog(_services, _stateAccessor, _serviceManager));
            AddDialog(new CancelDialog(_stateAccessor));
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
        }
    }
}
