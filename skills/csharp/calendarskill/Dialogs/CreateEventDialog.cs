using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Options;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Google.Apis.People.v1.Data;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Graph;
using Microsoft.Recognizers.Text.DateTime;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Dialogs
{
    public class CreateEventDialog : CalendarSkillDialogBase
    {
        public CreateEventDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            FindContactDialog findContactDialog,
            FindMeetingRoomDialog findMeetingRoomDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(CreateEventDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var createEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                RouteIntent,
                AfterRouteIntent,
                CollectAttendees,
                CollectTitle,
                //CollectContent,
                CollectStartDate,
                CollectStartTime,
                CollectDuration,
                CollectMeetingRoom,
                CollectLocation,
                ShowEventInfo,
                ConfirmBeforeCreatePrompt,
                AfterConfirmBeforeCreatePrompt,
                GetAuthToken,
                AfterGetAuthToken,
                CreateEvent,
            };

            var collectTitle = new WaterfallStep[]
            {
                CollectTitlePrompt,
                AfterCollectTitlePrompt
            };

            var collectContent = new WaterfallStep[]
            {
                CollectContentPrompt,
                AfterCollectContentPrompt
            };

            var updateStartDate = new WaterfallStep[]
            {
                UpdateStartDateForCreate,
                AfterUpdateStartDateForCreate,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTimeForCreate,
                AfterUpdateStartTimeForCreate,
            };

            var collectMeetingRoom = new WaterfallStep[]
            {
                CollectMeetingRoomPrompt,
                AfterCollectMeetingRoomPrompt
            };

            var collectLocation = new WaterfallStep[]
            {
                CollectLocationPrompt,
                AfterCollectLocationPrompt
            };

            var updateDuration = new WaterfallStep[]
            {
                UpdateDurationForCreate,
                AfterUpdateDurationForCreate,
            };

            var getRecreateInfo = new WaterfallStep[]
            {
                GetRecreateInfo,
                AfterGetRecreateInfo,
            };

            var showRestParticipants = new WaterfallStep[]
            {
                ShowRestParticipantsPrompt,
                AfterShowRestParticipantsPrompt,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CreateEvent, createEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTitle, collectTitle) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectContent, collectContent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectLocation, collectLocation) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectMeetingRoom, collectMeetingRoom) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowRestParticipants, showRestParticipants) { TelemetryClient = telemetryClient });
            AddDialog(new DatePrompt(Actions.DatePromptForCreate));
            AddDialog(new TimePrompt(Actions.TimePromptForCreate));
            AddDialog(new DurationPrompt(Actions.DurationPromptForCreate));
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            AddDialog(findContactDialog ?? throw new ArgumentNullException(nameof(findContactDialog)));
            AddDialog(findMeetingRoomDialog ?? throw new ArgumentNullException(nameof(findMeetingRoomDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.CreateEvent;
        }

        private async Task<DialogTurnResult> RouteIntent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.MeetingInfor.RecreateState != null)
                {
                    return await sc.NextAsync();
                }
                else if (state.InitialIntent == CalendarLuis.Intent.FindMeetingRoom || state.LuisResult.TopIntent().intent == CalendarLuis.Intent.CheckAvailability)
                {
                    return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), sc.Options, cancellationToken);
                }
                else if (state.InitialIntent == CalendarLuis.Intent.AddMeetingRoom || state.LuisResult.TopIntent().intent == CalendarLuis.Intent.ChangeMeetingRoom)
                {
                    return await sc.BeginDialogAsync(Actions.ChangeEventStatus);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterRouteIntent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.MeetingInfor.RecreateState != null)
                {
                    return await sc.NextAsync();
                }
                else if (state.InitialIntent == CalendarLuis.Intent.FindMeetingRoom || state.LuisResult.TopIntent().intent == CalendarLuis.Intent.CheckAvailability)
                {
                    if (state.MeetingInfor.MeetingRoom == null)
                    {
                        state.Clear();
                        return await sc.EndDialogAsync();
                    }
                }

                return await sc.NextAsync();
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectContent, sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectContentPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isContentSkipByDefault = false;
                isContentSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventContent")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(state.MeetingInfor.Content) && (!(state.MeetingInfor.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault()) || state.MeetingInfor.RecreateState == RecreateEventState.Content))
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(CreateEventResponses.NoContent) }, cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectContentPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isContentSkipByDefault = false;
                isContentSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventContent")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null && (!(state.MeetingInfor.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault()) || state.MeetingInfor.RecreateState == RecreateEventState.Content))
                {
                    if (string.IsNullOrEmpty(state.MeetingInfor.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        var merged_content = content != null ? content.ToString() : sc.Context.Activity.Text;
                        if (!CreateEventWhiteList.IsSkip(merged_content))
                        {
                            state.MeetingInfor.Content = merged_content;
                        }
                    }
                }
                else if (state.MeetingInfor.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault())
                {
                    state.MeetingInfor.Content = CalendarCommonStrings.DefaultContent;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectAttendees(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.RecreateState != null && state.MeetingInfor.RecreateState != RecreateEventState.Participants)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else if (state.MeetingInfor.ContactInfor.Contacts.Count == 0 || state.MeetingInfor.RecreateState == RecreateEventState.Participants)
                {
                    return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfor.MeetingRoom == null)
                {
                    return await sc.BeginDialogAsync(Actions.CollectLocation, sc.Options);
                }
                else
                {
                    state.MeetingInfor.Location = state.MeetingInfor.MeetingRoom.DisplayName;
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectLocationPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isLocationSkipByDefault = false;
                isLocationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventLocation")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfor.Location == null && (!(state.MeetingInfor.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault()) || state.MeetingInfor.RecreateState == RecreateEventState.Location))
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(CreateEventResponses.NoLocation) }, cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectLocationPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isLocationSkipByDefault = false;
                isLocationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventLocation")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.Location == null && sc.Result != null && (!(state.MeetingInfor.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault()) || state.MeetingInfor.RecreateState == RecreateEventState.Location))
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = state.LuisResult;

                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent.ToString();

                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                    // Enable the user to skip providing the location if they say something matching the Cancel intent, say something matching the ConfirmNo recognizer or something matching the NoLocation intent
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        state.MeetingInfor.Location = string.Empty;
                    }
                    else
                    {
                        state.MeetingInfor.Location = userInput;
                    }
                }
                else if (state.MeetingInfor.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault())
                {
                    state.MeetingInfor.Location = CalendarCommonStrings.DefaultLocation;
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectMeetingRoom(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if ((state.MeetingInfor.RecreateState == null && state.MeetingInfor.MeetingRoom == null) || state.MeetingInfor.RecreateState == RecreateEventState.MeetingRoom)
                {
                    state.MeetingInfor.MeetingRoom = null;
                    return await sc.BeginDialogAsync(Actions.CollectMeetingRoom, sc.Options);
                }
                else
                {
                    return await sc.NextAsync();
                }

            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isLocationSkipByDefault = false;
                isLocationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventLocation")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.RecreateState == RecreateEventState.MeetingRoom)
                {
                    return await sc.NextAsync();
                }
                else if (state.MeetingInfor.MeetingRoom == null && (!(state.MeetingInfor.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault())))
                {
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                    {
                        Prompt = ResponseManager.GetResponse(CreateEventResponses.AddMeetingRoom),
                    }, cancellationToken);
                }
                else
                {
                    return await sc.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectMeetingRoomPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfor.RecreateState == RecreateEventState.MeetingRoom)
                {
                    return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), options: sc.Options, cancellationToken: cancellationToken);
                }

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), options: sc.Options, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ShowEventInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // show event information before create
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var source = state.EventSource;
                var newEvent = new EventModel(source)
                {
                    Title = state.MeetingInfor.Title,
                    Content = state.MeetingInfor.Content,
                    Attendees = state.MeetingInfor.ContactInfor.Contacts,
                    StartTime = state.MeetingInfor.StartDateTime.Value,
                    EndTime = state.MeetingInfor.EndDateTime.Value,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.MeetingInfor.Location,
                    ContentPreview = state.MeetingInfor.Content
                };

                var attendeeConfirmTextString = string.Empty;
                if (state.MeetingInfor.ContactInfor.Contacts.Count > 0)
                {
                    var attendeeConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateAttendees, new StringDictionary()
                    {
                        { "Attendees", DisplayHelper.ToDisplayParticipantsStringSummary(state.MeetingInfor.ContactInfor.Contacts, 5) }
                    });
                    attendeeConfirmTextString = attendeeConfirmResponse.Text;
                }

                var subjectConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfor.Title))
                {
                    var subjectConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateSubject, new StringDictionary()
                    {
                        { "Subject", string.IsNullOrEmpty(state.MeetingInfor.Title) ? CalendarCommonStrings.Empty : state.MeetingInfor.Title }
                    });
                    subjectConfirmString = state.MeetingInfor.Title == CreateEventWhiteList.GetDefaultTitle() ? CalendarCommonStrings.NoSubject : subjectConfirmResponse.Text;
                }

                var locationConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfor.Location))
                {
                    var subjectConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateLocation, new StringDictionary()
                    {
                        { "Location", string.IsNullOrEmpty(state.MeetingInfor.Location) ? CalendarCommonStrings.Empty : state.MeetingInfor.MeetingRoom == null ? state.MeetingInfor.Location : state.MeetingInfor.MeetingRoom.DisplayName },
                    });
                    locationConfirmString = subjectConfirmResponse.Text;
                }

                var contentConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfor.Content))
                {
                    var contentConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateContent, new StringDictionary()
                    {
                        { "Content", string.IsNullOrEmpty(state.MeetingInfor.Content) ? CalendarCommonStrings.Empty : state.MeetingInfor.Content },
                    });
                    contentConfirmString = contentConfirmResponse.Text;
                }

                var startDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());
                var endDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(state.MeetingInfor.EndDateTime.Value, state.GetUserTimeZone());
                var tokens = new StringDictionary
                {
                    { "AttendeesConfirm", attendeeConfirmTextString },
                    { "Date", startDateTimeInUserTimeZone.ToSpeechDateString(false).ToLower() },
                    { "Time", startDateTimeInUserTimeZone.ToSpeechTimeString(false) },
                    { "EndTime", endDateTimeInUserTimeZone.ToSpeechTimeString(false) },
                    { "SubjectConfirm", subjectConfirmString },
                    { "LocationConfirm", locationConfirmString },
                    { "ContentConfirm", contentConfirmString },
                };

                var prompt = await GetDetailMeetingResponseAsync(sc, newEvent, CreateEventResponses.ConfirmCreate, tokens);

                await sc.Context.SendActivityAsync(prompt);

                // show at most 5 user names, ask user show rest users
                if (state.MeetingInfor.ContactInfor.Contacts.Count > 5)
                {
                    return await sc.BeginDialogAsync(Actions.ShowRestParticipants);
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmBeforeCreatePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreatePrompt),
                    RetryPrompt = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateFailed)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmBeforeCreatePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync();
                }
                else
                {
                    // if user not create, ask if user want to change any field
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CreateEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var source = state.EventSource;

                if (state.MeetingInfor.MeetingRoom != null)
                {
                    state.MeetingInfor.ContactInfor.Contacts.Add(new EventModel.Attendee
                    {
                        DisplayName = state.MeetingInfor.MeetingRoom.DisplayName,
                        Address = state.MeetingInfor.MeetingRoom.EmailAddress,
                        AttendeeType = AttendeeType.Resource
                    });
                }

                var newEvent = new EventModel(source)
                {
                    Title = state.MeetingInfor.Title,
                    Content = state.MeetingInfor.Content,
                    Attendees = state.MeetingInfor.ContactInfor.Contacts,
                    StartTime = (DateTime)state.MeetingInfor.StartDateTime,
                    EndTime = (DateTime)state.MeetingInfor.EndDateTime,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.MeetingInfor.MeetingRoom == null ? state.MeetingInfor.Location : null,
                };

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                if (await calendarService.CreateEventAysnc(newEvent) != null)
                {
                    /*
                    var tokens = new StringDictionary
                    {
                        { "Subject", state.MeetingInfor.Title },
                    };

                    newEvent.ContentPreview = state.MeetingInfor.Content;

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, CreateEventResponses.EventCreated, tokens);

                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                    */

                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CreateEventResponses.MeetingBooked));
                }
                else
                {
                    var prompt = ResponseManager.GetResponse(CreateEventResponses.EventCreationFailed);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }

                state.Clear();

                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CreateEventResponses.GetRecreateInfo),
                    RetryPrompt = ResponseManager.GetResponse(CreateEventResponses.GetRecreateInfoRetry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterGetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null)
                {
                    var recreateState = sc.Result as RecreateEventState?;
                    switch (recreateState.Value)
                    {
                        case RecreateEventState.Cancel:
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded), cancellationToken);
                            state.Clear();
                            return await sc.EndDialogAsync(true, cancellationToken);
                        case RecreateEventState.Time:
                            state.MeetingInfor.ClearTimesForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Duration:
                            state.MeetingInfor.ClearEndTimesAndDurationForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.MeetingRoom:
                            state.MeetingInfor.ClearMeetingRoomForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Location:
                            state.MeetingInfor.ClearLocationForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Participants:
                            state.MeetingInfor.ClearParticipantsForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Subject:
                            state.MeetingInfor.ClearSubjectForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Content:
                            state.MeetingInfor.ClearContentForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        default:
                            // should not go to this part. place an error handling for save.
                            await HandleDialogExceptions(sc, new Exception("Get unexpect state in recreate."));
                            return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                    }
                }
                else
                {
                    // should not go to this part. place an error handling for safe.
                    await HandleDialogExceptions(sc, new Exception("Get unexpect result in recreate."));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ShowRestParticipantsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CreateEventResponses.ShowRestParticipantsPrompt),
                    RetryPrompt = ResponseManager.GetResponse(CreateEventResponses.ShowRestParticipantsPrompt)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterShowRestParticipantsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    await sc.Context.SendActivityAsync(state.MeetingInfor.ContactInfor.Contacts.GetRange(5, state.MeetingInfor.ContactInfor.Contacts.Count - 5).ToSpeechString(CommonStrings.And, li => li.DisplayName ?? li.Address));
                }

                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}