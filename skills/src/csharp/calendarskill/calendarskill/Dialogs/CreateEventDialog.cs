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
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
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
                CollectAttendees,
                CollectTitle,
                CollectContent,
                CollectStartDate,
                CollectStartTime,
                CollectDuration,
                CollectLocation,
                ConfirmBeforeCreate,
                ConfirmBeforeCreatePrompt,
                CreateEvent,
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
                ShowRestParticipants,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CreateEvent, createEvent) { TelemetryClient = telemetryClient });
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

            // Set starting dialog for component
            InitialDialogId = Actions.CreateEvent;
        }

        // Create Event waterfall steps
        public async Task<DialogTurnResult> CollectTitle(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isTitleSkipByDefault = false;
                isTitleSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventTitle")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfor.RecreateState == RecreateEventState.Subject)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(CreateEventResponses.NoTitleShort) }, cancellationToken);
                }
                else if (state.MeetingInfor.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault())
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else if (string.IsNullOrEmpty(state.MeetingInfor.Title))
                {
                    if (state.MeetingInfor.ContactInfor.Contacts.Count == 0 || state.MeetingInfor.ContactInfor.Contacts == null)
                    {
                        state.Clear();
                        return await sc.EndDialogAsync(true);
                    }

                    var userNameString = state.MeetingInfor.ContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.DisplayName ?? li.Address}: {li.Address}");
                    var data = new StringDictionary() { { "UserName", userNameString } };
                    var prompt = ResponseManager.GetResponse(CreateEventResponses.NoTitle, data);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
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

        public async Task<DialogTurnResult> CollectContent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isTitleSkipByDefault = false;
                isTitleSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventTitle")?.IsSkipByDefault;

                bool? isContentSkipByDefault = false;
                isContentSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventContent")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null || (state.MeetingInfor.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault()) || state.MeetingInfor.RecreateState == RecreateEventState.Subject)
                {
                    if (string.IsNullOrEmpty(state.MeetingInfor.Title))
                    {
                        if (state.MeetingInfor.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault() && state.MeetingInfor.RecreateState != RecreateEventState.Subject)
                        {
                            state.MeetingInfor.Title = CreateEventWhiteList.GetDefaultTitle();
                        }
                        else
                        {
                            sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                            var title = content != null ? content.ToString() : sc.Context.Activity.Text;
                            if (CreateEventWhiteList.IsSkip(title))
                            {
                                state.MeetingInfor.Title = CreateEventWhiteList.GetDefaultTitle();
                            }
                            else
                            {
                                state.MeetingInfor.Title = title;
                            }
                        }
                    }
                }

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

        public async Task<DialogTurnResult> CollectAttendees(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                if (state.MeetingInfor.ContactInfor.Contacts.Count == 0 || state.MeetingInfor.RecreateState == RecreateEventState.Participants)
                {
                    return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
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

        public async Task<DialogTurnResult> CollectStartDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                if (!state.MeetingInfor.StartDate.Any())
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound), cancellationToken);
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

        public async Task<DialogTurnResult> CollectStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.RecreateState == null || state.MeetingInfor.RecreateState == RecreateEventState.Time)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound), cancellationToken);
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

        public async Task<DialogTurnResult> CollectDuration(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfor.EndDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound), cancellationToken);
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

        public async Task<DialogTurnResult> CollectLocation(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> ConfirmBeforeCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    subjectConfirmString = subjectConfirmResponse.Text;
                }

                var locationConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfor.Location))
                {
                    var subjectConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateLocation, new StringDictionary()
                    {
                        { "Location", string.IsNullOrEmpty(state.MeetingInfor.Location) ? CalendarCommonStrings.Empty : state.MeetingInfor.Location },
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
                    { "Date", startDateTimeInUserTimeZone.ToSpeechDateString(false) },
                    { "Time", startDateTimeInUserTimeZone.ToSpeechTimeString(false) },
                    { "EndTime", endDateTimeInUserTimeZone.ToSpeechTimeString(false) },
                    { "SubjectConfirm", subjectConfirmString },
                    { "LocationConfirm", locationConfirmString },
                    { "ContentConfirm", contentConfirmString },
                };

                var prompt = await GetDetailMeetingResponseAsync(sc, newEvent, CreateEventResponses.ConfirmCreate, tokens);

                await sc.Context.SendActivityAsync(prompt);

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

        public async Task<DialogTurnResult> ConfirmBeforeCreatePrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> CreateEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var source = state.EventSource;
                    var newEvent = new EventModel(source)
                    {
                        Title = state.MeetingInfor.Title,
                        Content = state.MeetingInfor.Content,
                        Attendees = state.MeetingInfor.ContactInfor.Contacts,
                        StartTime = (DateTime)state.MeetingInfor.StartDateTime,
                        EndTime = (DateTime)state.MeetingInfor.EndDateTime,
                        TimeZone = TimeZoneInfo.Utc,
                        Location = state.MeetingInfor.Location,
                    };

                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);
                    if (await calendarService.CreateEventAysnc(newEvent) != null)
                    {
                        var tokens = new StringDictionary
                        {
                            { "Subject", state.MeetingInfor.Title },
                        };

                        newEvent.ContentPreview = state.MeetingInfor.Content;

                        var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, CreateEventResponses.EventCreated, tokens);

                        await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                    }
                    else
                    {
                        var prompt = ResponseManager.GetResponse(CreateEventResponses.EventCreationFailed);
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                    }

                    state.Clear();
                }
                else
                {
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken: cancellationToken);
                }

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

        // update start date waterfall steps
        public async Task<DialogTurnResult> UpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isStartDateSkipByDefault = false;
                isStartDateSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventStartDate")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.CreateHasDetail && isStartDateSkipByDefault.GetValueOrDefault() && state.MeetingInfor.RecreateState != RecreateEventState.Time)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.DatePromptForCreate, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CreateEventResponses.NoStartDate),
                    RetryPrompt = ResponseManager.GetResponse(CreateEventResponses.NoStartDateRetry),
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartDateForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isStartDateSkipByDefault = false;
                isStartDateSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventStartDate")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.CreateHasDetail && isStartDateSkipByDefault.GetValueOrDefault() && state.MeetingInfor.RecreateState != RecreateEventState.Time)
                {
                    var datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
                    var defaultValue = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventStartDate")?.DefaultValue;
                    if (int.TryParse(defaultValue, out var startDateOffset))
                    {
                        datetime = datetime.AddDays(startDateOffset);
                    }

                    state.MeetingInfor.StartDate.Add(datetime);
                }
                else
                if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    foreach (var resolution in dateTimeResolutions)
                    {
                        var dateTimeConvertType = resolution?.Timex;
                        var dateTimeValue = resolution?.Value;
                        if (dateTimeValue != null)
                        {
                            try
                            {
                                var dateTime = DateTime.Parse(dateTimeValue);

                                if (dateTime != null)
                                {
                                    var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeValue, dateTimeConvertType);
                                    if (ContainsTime(dateTimeConvertType))
                                    {
                                        state.MeetingInfor.StartTime.Add(TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()));
                                    }

                                    // Workaround as DateTimePrompt only return as local time
                                    if (isRelativeTime)
                                    {
                                        dateTime = new DateTime(
                                            dateTime.Year,
                                            dateTime.Month,
                                            dateTime.Day,
                                            DateTime.Now.Hour,
                                            DateTime.Now.Minute,
                                            DateTime.Now.Second);
                                    }

                                    state.MeetingInfor.StartDate.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime);
                                }
                            }
                            catch (FormatException ex)
                            {
                                await HandleExpectedDialogExceptions(sc, ex);
                            }
                        }
                    }
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // update start time waterfall steps
        public async Task<DialogTurnResult> UpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!state.MeetingInfor.StartTime.Any())
                {
                    return await sc.PromptAsync(Actions.TimePromptForCreate, new NoSkipPromptOptions
                    {
                        Prompt = ResponseManager.GetResponse(CreateEventResponses.NoStartTime),
                        RetryPrompt = ResponseManager.GetResponse(CreateEventResponses.NoStartTimeRetry),
                        NoSkipPrompt = ResponseManager.GetResponse(CreateEventResponses.NoStartTimeNoSkip),
                    }, cancellationToken);
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

        public async Task<DialogTurnResult> AfterUpdateStartTimeForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null && !state.MeetingInfor.StartTime.Any())
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    foreach (var resolution in dateTimeResolutions)
                    {
                        var dateTimeConvertType = resolution?.Timex;
                        var dateTimeValue = resolution?.Value;
                        if (dateTimeValue != null)
                        {
                            try
                            {
                                var dateTime = DateTime.Parse(dateTimeValue);

                                if (dateTime != null)
                                {
                                    var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, dateTimeValue, dateTimeConvertType);
                                    state.MeetingInfor.StartTime.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, state.GetUserTimeZone()) : dateTime);
                                }
                            }
                            catch (FormatException ex)
                            {
                                await HandleExpectedDialogExceptions(sc, ex);
                            }
                        }
                    }
                }

                var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());
                var startDate = state.MeetingInfor.StartDate.Last();
                foreach (var startTime in state.MeetingInfor.StartTime)
                {
                    var startDateTime = new DateTime(
                        startDate.Year,
                        startDate.Month,
                        startDate.Day,
                        startTime.Hour,
                        startTime.Minute,
                        startTime.Second);
                    if (state.MeetingInfor.StartDateTime == null)
                    {
                        state.MeetingInfor.StartDateTime = startDateTime;
                    }

                    if (startDateTime >= userNow)
                    {
                        state.MeetingInfor.StartDateTime = startDateTime;
                        break;
                    }
                }

                state.MeetingInfor.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfor.StartDateTime.Value, state.GetUserTimeZone());
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // update duration waterfall steps
        public async Task<DialogTurnResult> UpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isDurationSkipByDefault = false;
                isDurationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventDuration")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.Duration > 0 || state.MeetingInfor.EndTime.Any() || state.MeetingInfor.EndDate.Any() || (state.MeetingInfor.CreateHasDetail && isDurationSkipByDefault.GetValueOrDefault() && state.MeetingInfor.RecreateState != RecreateEventState.Time && state.MeetingInfor.RecreateState != RecreateEventState.Duration))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.DurationPromptForCreate, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(CreateEventResponses.NoDuration),
                    RetryPrompt = ResponseManager.GetResponse(CreateEventResponses.NoDurationRetry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateDurationForCreate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isDurationSkipByDefault = false;
                isDurationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventDuration")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfor.EndDate.Any() || state.MeetingInfor.EndTime.Any())
                {
                    var startDate = !state.MeetingInfor.StartDate.Any() ? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone()) : state.MeetingInfor.StartDate.Last();
                    var endDate = startDate;
                    if (state.MeetingInfor.EndDate.Any())
                    {
                        endDate = state.MeetingInfor.EndDate.Last();
                    }

                    if (state.MeetingInfor.EndTime.Any())
                    {
                        foreach (var endtime in state.MeetingInfor.EndTime)
                        {
                            var endDateTime = new DateTime(
                                endDate.Year,
                                endDate.Month,
                                endDate.Day,
                                endtime.Hour,
                                endtime.Minute,
                                endtime.Second);
                            endDateTime = TimeZoneInfo.ConvertTimeToUtc(endDateTime, state.GetUserTimeZone());
                            if (state.MeetingInfor.EndDateTime == null || endDateTime >= state.MeetingInfor.StartDateTime)
                            {
                                state.MeetingInfor.EndDateTime = endDateTime;
                            }
                        }
                    }
                    else
                    {
                        state.MeetingInfor.EndDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
                        state.MeetingInfor.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfor.EndDateTime.Value, state.GetUserTimeZone());
                    }

                    var ts = state.MeetingInfor.StartDateTime.Value.Subtract(state.MeetingInfor.EndDateTime.Value).Duration();
                    state.MeetingInfor.Duration = (int)ts.TotalSeconds;
                }

                if (state.MeetingInfor.Duration <= 0 && state.MeetingInfor.CreateHasDetail && isDurationSkipByDefault.GetValueOrDefault() && state.MeetingInfor.RecreateState != RecreateEventState.Time && state.MeetingInfor.RecreateState != RecreateEventState.Duration)
                {
                    var defaultValue = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventDuration")?.DefaultValue;
                    if (int.TryParse(defaultValue, out var durationMinutes))
                    {
                        state.MeetingInfor.Duration = durationMinutes * 60;
                    }
                    else
                    {
                        state.MeetingInfor.Duration = 1800;
                    }
                }

                if (state.MeetingInfor.Duration <= 0 && sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);

                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    if (dateTimeResolutions.First().Value != null)
                    {
                        int.TryParse(dateTimeResolutions.First().Value, out var duration);
                        state.MeetingInfor.Duration = duration;
                    }
                }

                if (state.MeetingInfor.Duration > 0)
                {
                    state.MeetingInfor.EndDateTime = state.MeetingInfor.StartDateTime.Value.AddSeconds(state.MeetingInfor.Duration);
                }
                else
                {
                    // should not go to this part in current logic.
                    // place an error handling for save.
                    await HandleDialogExceptions(sc, new Exception("Unexpect Error On get duration"));
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> GetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> AfterGetRecreateInfo(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    // should not go to this part. place an error handling for save.
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

        public async Task<DialogTurnResult> ShowRestParticipantsPrompt(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        public async Task<DialogTurnResult> ShowRestParticipants(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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