using System;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill
{
    public class RootDialog : RouterDialog
    {
        private const string CancelCode = "cancel";
        private bool _skillMode;
        private CalendarSkillAccessors _accessors;
        private CalendarSkillResponses _responder;
        private CalendarSkillServices _services;
        private IServiceManager _serviceManager;

        public RootDialog(bool skillMode, CalendarSkillServices services, CalendarSkillAccessors calendarBotAccessors, IServiceManager serviceManager)
            : base(nameof(RootDialog))
        {
            _skillMode = skillMode;
            _accessors = calendarBotAccessors;
            _serviceManager = serviceManager;
            _responder = new CalendarSkillResponses();
            _services = services;

            // Initialise dialogs
            RegisterDialogs();
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the conversation state from the turn context
            var state = await _accessors.CalendarSkillState.GetAsync(dc.Context, () => new CalendarSkillState());
            var dialogState = await _accessors.ConversationDialogState.GetAsync(dc.Context, () => new DialogState());

            Calendar luisResult = null;

            if (_skillMode && state.LuisResultPassedFromSkill != null)
            {
                // If invoked by a Skill we get the Luis IRecognizerConvert passed to us on first turn so we don't have to do that locally
                luisResult = (Calendar)state.LuisResultPassedFromSkill;
                state.LuisResultPassedFromSkill = null;
            }
            else if (_services?.LuisRecognizer != null)
            {
                // When run in normal mode or 2+ turn of a skill we use LUIS ourselves as the parent Dispatcher doesn't do this
                luisResult = await _services.LuisRecognizer.RecognizeAsync<Calendar>(dc.Context, cancellationToken);
            }
            else
            {
                throw new Exception("CalendarSkill: Could not get Luis Recognizer result.");
            }

            var intent = luisResult?.TopIntent().intent;

            var skillOptions = new CalendarSkillDialogOptions
            {
                SkillMode = _skillMode,
            };

            switch (intent)
            {
                case Calendar.Intent.Greeting:
                    {
                        await dc.BeginDialogAsync(GreetingDialog.Name, skillOptions);
                        break;
                    }

                case Calendar.Intent.FindMeetingRoom:
                case Calendar.Intent.CreateCalendarEntry:
                    {
                        await dc.BeginDialogAsync(CreateEventDialog.Name, skillOptions);
                        break;
                    }

                case Calendar.Intent.DeleteCalendarEntry:
                    {
                        await dc.BeginDialogAsync(DeleteEventDialog.Name, skillOptions);
                        break;
                    }

                case Calendar.Intent.NextMeeting:
                    {
                        await dc.BeginDialogAsync(NextMeetingDialog.Name, skillOptions);
                        break;
                    }

                case Calendar.Intent.ChangeCalendarEntry:
                    {
                        await dc.BeginDialogAsync(UpdateEventDialog.Name, skillOptions);
                        break;
                    }

                case Calendar.Intent.FindCalendarEntry:
                case Calendar.Intent.ShowNext:
                case Calendar.Intent.ShowPrevious:
                case Calendar.Intent.Summary:
                    {
                        await dc.BeginDialogAsync(SummaryDialog.Name, skillOptions);
                        break;
                    }

                case Calendar.Intent.None:
                    {
                        await _responder.ReplyWith(dc.Context, CalendarSkillResponses.Confused);
                        if (_skillMode)
                        {
                            await CompleteAsync(dc);
                        }

                        break;
                    }

                default:
                    {
                        await dc.Context.SendActivityAsync("This feature is not yet available in the Calendar Skill. Please try asking something else.");
                        if (_skillMode)
                        {
                            await CompleteAsync(dc);
                        }
                        break;
                    }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (result?.Result != null && result.Result.ToString() == "StartNew")
            {
                await this.RouteAsync(dc);
            }
            else
            {
                var response = dc.Context.Activity.CreateReply();
                response.Type = ActivityTypes.EndOfConversation;

                await dc.Context.SendActivityAsync(response);

                // End active dialog
                await dc.EndDialogAsync(result);
            }
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Name == "skillBegin")
            {
                var state = await _accessors.CalendarSkillState.GetAsync(dc.Context, () => new CalendarSkillState());
                var skillMetadata = dc.Context.Activity.Value as SkillMetadata;

                if (skillMetadata != null)
                {
                    var luisService = skillMetadata.LuisService;
                    var luisApp = new LuisApplication(luisService.AppId, luisService.SubscriptionKey, luisService.GetEndpoint());
                    _services.LuisRecognizer = new LuisRecognizer(luisApp);

                    state.LuisResultPassedFromSkill = skillMetadata.LuisResult;
                    if (state.UserInfo == null)
                    {
                        state.UserInfo = new CalendarSkillState.UserInformation();
                    }

                    // Each skill is configured to explictly request certain items to be passed across
                    if (skillMetadata.Parameters.TryGetValue("IPA.Timezone", out var timezone))
                    {
                        // we have a timezone
                        state.UserInfo.Timezone = (TimeZoneInfo)timezone;
                    }
                    else
                    {
                        // TODO Error handling if parameter isn't passed (or default)
                    }
                }

                state.EventSource = EventSource.Microsoft;
            }
            else if (dc.Context.Activity.Name == "tokens/response")
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
            }
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;
            var reply = activity.CreateReply(CalendarBotResponses.CalendarWelcomeMessage);
            await dc.Context.SendActivityAsync(reply);
        }

        protected override async Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Context.Activity.Text?.ToLower() == CancelCode)
            {
                await CompleteAsync(dc);

                return InterruptionAction.StartedDialog;
            }
            else
            {
                if (!this._skillMode && dc.Context.Activity.Type == ActivityTypes.Message)
                {
                    var luisResult = await this._services.LuisRecognizer.RecognizeAsync<Calendar>(dc.Context, cancellationToken);
                    var topIntent = luisResult.TopIntent().intent;

                    // check intent
                    switch (topIntent)
                    {
                        case Calendar.Intent.Cancel:
                            {
                                return await this.OnCancel(dc);
                            }

                        case Calendar.Intent.Help:
                            {
                                return await this.OnHelp(dc);
                            }

                        case Calendar.Intent.Logout:
                            {
                                return await this.OnLogout(dc);
                            }
                    }
                }

                return InterruptionAction.NoAction;
            }
        }

        private async Task<InterruptionAction> OnCancel(DialogContext dc)
        {
            var cancelling = dc.Context.Activity.CreateReply(CalendarBotResponses.CancellingMessage);
            await dc.Context.SendActivityAsync(cancelling);

            var state = await _accessors.CalendarSkillState.GetAsync(dc.Context);
            state.Clear();

            await dc.CancelAllDialogsAsync();

            return InterruptionAction.StartedDialog;
        }

        private async Task<InterruptionAction> OnHelp(DialogContext dc)
        {
            var helpMessage = dc.Context.Activity.CreateReply(CalendarBotResponses.HelpMessage);
            await dc.Context.SendActivityAsync(helpMessage);

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
            // await adapter.SignOutUserAsync(dc.Context, "googleapi", default(CancellationToken)).ConfigureAwait(false);
            await adapter.SignOutUserAsync(dc.Context, "msgraph");
            var logoutMessage = dc.Context.Activity.CreateReply(CalendarBotResponses.LogOut);
            await dc.Context.SendActivityAsync(logoutMessage);

            var state = await _accessors.CalendarSkillState.GetAsync(dc.Context);
            state.Clear();

            return InterruptionAction.StartedDialog;
        }

        private void RegisterDialogs()
        {
            AddDialog(new CreateEventDialog(_services, _accessors, _serviceManager));
            AddDialog(new DeleteEventDialog(_services, _accessors, _serviceManager));
            AddDialog(new NextMeetingDialog(_services, _accessors, _serviceManager));
            AddDialog(new UpdateEventDialog(_services, _accessors, _serviceManager));
            AddDialog(new SummaryDialog(_services, _accessors, _serviceManager));
            AddDialog(new GreetingDialog(_services, _accessors, _serviceManager));
        }
    }
}
