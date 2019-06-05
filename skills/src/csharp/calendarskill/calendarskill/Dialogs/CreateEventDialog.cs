using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogModel;
using CalendarSkill.Options;
using CalendarSkill.Prompts;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
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
               IBotTelemetryClient telemetryClient)
               : base(nameof(CreateEventDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var createEvent = new WaterfallStep[]
            {
                InitCreateEventDialogState,
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
                SaveCreateEventDialogState,
                UpdateStartDateForCreate,
                AfterUpdateStartDateForCreate,
            };

            var updateStartTime = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                UpdateStartTimeForCreate,
                AfterUpdateStartTimeForCreate,
            };

            var updateDuration = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                UpdateDurationForCreate,
                AfterUpdateDurationForCreate,
            };

            var getRecreateInfo = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                GetRecreateInfo,
                AfterGetRecreateInfo,
            };

            var showRestParticipants = new WaterfallStep[]
            {
                SaveCreateEventDialogState,
                ShowRestParticipantsPrompt,
                ShowRestParticipants,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new CalendarWaterfallDialog(Actions.CreateEvent, createEvent, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.UpdateDurationForCreate, updateDuration, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.ShowRestParticipants, showRestParticipants, CalendarStateAccessor) { TelemetryClient = telemetryClient });
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
                if (sc.Result != null && sc.Result is FindContactDialogOptions)
                {
                    var result = (FindContactDialogOptions)sc.Result;
                    sc.State.Dialog[CalendarStateKey] = result.DialogState;
                }

                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.RecreateState == RecreateEventState.Subject)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(CreateEventResponses.NoTitleShort) }, cancellationToken);
                }
                else
                if (string.IsNullOrEmpty(dialogState.Title))
                {
                    if (dialogState.FindContactInfor.Contacts.Count == 0 || dialogState.FindContactInfor.Contacts == null)
                    {
                        dialogState.FindContactInfor.FirstRetryInFindContact = true;
                        return await sc.EndDialogAsync();
                    }

                    var userNameString = dialogState.FindContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.DisplayName ?? li.Address}: {li.Address}");
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (sc.Result != null || dialogState.CreateHasDetail || dialogState.RecreateState == RecreateEventState.Subject)
                {
                    if (string.IsNullOrEmpty(dialogState.Title))
                    {
                        if (dialogState.CreateHasDetail && dialogState.RecreateState != RecreateEventState.Subject)
                        {
                            dialogState.Title = CreateEventWhiteList.GetDefaultTitle();
                        }
                        else
                        {
                            sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                            var title = content != null ? content.ToString() : sc.Context.Activity.Text;
                            if (CreateEventWhiteList.IsSkip(title))
                            {
                                dialogState.Title = CreateEventWhiteList.GetDefaultTitle();
                            }
                            else
                            {
                                dialogState.Title = title;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(dialogState.Content) && (!dialogState.CreateHasDetail || dialogState.RecreateState == RecreateEventState.Content))
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (string.IsNullOrEmpty(userState.APIToken))
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);

                if (dialogState.FindContactInfor.Contacts.Count == 0 || dialogState.RecreateState == RecreateEventState.Participants)
                {
                    return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(skillOptions), cancellationToken: cancellationToken);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (sc.Result != null && (!dialogState.CreateHasDetail || dialogState.RecreateState == RecreateEventState.Content))
                {
                    if (string.IsNullOrEmpty(dialogState.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        var merged_content = content != null ? content.ToString() : sc.Context.Activity.Text;
                        if (!CreateEventWhiteList.IsSkip(merged_content))
                        {
                            dialogState.Content = merged_content;
                        }
                    }
                }

                if (!dialogState.StartDate.Any())
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartDateForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound, skillOptions), cancellationToken);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.RecreateState == null || dialogState.RecreateState == RecreateEventState.Time)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateStartTimeForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound, skillOptions), cancellationToken);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.EndDateTime == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateDurationForCreate, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound, skillOptions), cancellationToken);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.Location == null && (!dialogState.CreateHasDetail || dialogState.RecreateState == RecreateEventState.Location))
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.Location == null && sc.Result != null && (!dialogState.CreateHasDetail || dialogState.RecreateState == RecreateEventState.Location))
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = userState.LuisResult;

                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent.ToString();

                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                    // Enable the user to skip providing the location if they say something matching the Cancel intent, say something matching the ConfirmNo recognizer or something matching the NoLocation intent
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        dialogState.Location = string.Empty;
                    }
                    else
                    {
                        dialogState.Location = userInput;
                    }
                }

                var source = userState.EventSource;
                var newEvent = new EventModel(source)
                {
                    Title = dialogState.Title,
                    Content = dialogState.Content,
                    Attendees = dialogState.FindContactInfor.Contacts,
                    StartTime = dialogState.StartDateTime.Value,
                    EndTime = dialogState.EndDateTime.Value,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = dialogState.Location,
                    ContentPreview = dialogState.Content
                };

                var attendeeConfirmTextString = string.Empty;
                if (dialogState.FindContactInfor.Contacts.Count > 0)
                {
                    var attendeeConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateAttendees, new StringDictionary()
                    {
                        { "Attendees", DisplayHelper.ToDisplayParticipantsStringSummary(dialogState.FindContactInfor.Contacts, 5) }
                    });
                    attendeeConfirmTextString = attendeeConfirmResponse.Text;
                }

                var subjectConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(dialogState.Title))
                {
                    var subjectConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateSubject, new StringDictionary()
                    {
                        { "Subject", string.IsNullOrEmpty(dialogState.Title) ? CalendarCommonStrings.Empty : dialogState.Title }
                    });
                    subjectConfirmString = subjectConfirmResponse.Text;
                }

                var locationConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(dialogState.Location))
                {
                    var subjectConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateLocation, new StringDictionary()
                    {
                        { "Location", string.IsNullOrEmpty(dialogState.Location) ? CalendarCommonStrings.Empty : dialogState.Location },
                    });
                    locationConfirmString = subjectConfirmResponse.Text;
                }

                var contentConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(dialogState.Content))
                {
                    var contentConfirmResponse = ResponseManager.GetResponse(CreateEventResponses.ConfirmCreateContent, new StringDictionary()
                    {
                        { "Content", string.IsNullOrEmpty(dialogState.Content) ? CalendarCommonStrings.Empty : dialogState.Content },
                    });
                    contentConfirmString = contentConfirmResponse.Text;
                }

                var startDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(dialogState.StartDateTime.Value, userState.GetUserTimeZone());
                var endDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(dialogState.EndDateTime.Value, userState.GetUserTimeZone());
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

                if (dialogState.FindContactInfor.Contacts.Count > 5)
                {
                    return await sc.BeginDialogAsync(Actions.ShowRestParticipants, skillOptions);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var source = userState.EventSource;
                    var newEvent = new EventModel(source)
                    {
                        Title = dialogState.Title,
                        Content = dialogState.Content,
                        Attendees = dialogState.FindContactInfor.Contacts,
                        StartTime = (DateTime)dialogState.StartDateTime,
                        EndTime = (DateTime)dialogState.EndDateTime,
                        TimeZone = TimeZoneInfo.Utc,
                        Location = dialogState.Location,
                    };

                    var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);
                    if (await calendarService.CreateEvent(newEvent) != null)
                    {
                        var tokens = new StringDictionary
                        {
                            { "Subject", dialogState.Title },
                        };

                        newEvent.ContentPreview = dialogState.Content;

                        var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, CreateEventResponses.EventCreated, tokens);

                        await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                    }
                    else
                    {
                        var prompt = ResponseManager.GetResponse(CreateEventResponses.EventCreationFailed);
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                    }

                    await ClearAllState(sc.Context);
                }
                else
                {
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, skillOptions, cancellationToken: cancellationToken);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.CreateHasDetail && dialogState.RecreateState != RecreateEventState.Time)
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.CreateHasDetail && dialogState.RecreateState != RecreateEventState.Time)
                {
                    var datetime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userState.GetUserTimeZone());
                    dialogState.StartDate.Add(datetime);
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
                                        dialogState.StartTime.Add(TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, userState.GetUserTimeZone()));
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

                                    dialogState.StartDate.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, userState.GetUserTimeZone()) : dateTime);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (!dialogState.StartTime.Any())
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (sc.Result != null && !dialogState.StartTime.Any())
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
                                    dialogState.StartTime.Add(isRelativeTime ? TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local, userState.GetUserTimeZone()) : dateTime);
                                }
                            }
                            catch (FormatException ex)
                            {
                                await HandleExpectedDialogExceptions(sc, ex);
                            }
                        }
                    }
                }

                var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userState.GetUserTimeZone());
                var startDate = dialogState.StartDate.Last();
                foreach (var startTime in dialogState.StartTime)
                {
                    var startDateTime = new DateTime(
                        startDate.Year,
                        startDate.Month,
                        startDate.Day,
                        startTime.Hour,
                        startTime.Minute,
                        startTime.Second);
                    if (dialogState.StartDateTime == null)
                    {
                        dialogState.StartDateTime = startDateTime;
                    }

                    if (startDateTime >= userNow)
                    {
                        dialogState.StartDateTime = startDateTime;
                        break;
                    }
                }

                dialogState.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(dialogState.StartDateTime.Value, userState.GetUserTimeZone());
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.Duration > 0 || dialogState.EndTime.Any() || dialogState.EndDate.Any() || (dialogState.CreateHasDetail && dialogState.RecreateState != RecreateEventState.Time && dialogState.RecreateState != RecreateEventState.Duration))
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.EndDate.Any() || dialogState.EndTime.Any())
                {
                    var startDate = !dialogState.StartDate.Any() ? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userState.GetUserTimeZone()) : dialogState.StartDate.Last();
                    var endDate = startDate;
                    if (dialogState.EndDate.Any())
                    {
                        endDate = dialogState.EndDate.Last();
                    }

                    if (dialogState.EndTime.Any())
                    {
                        foreach (var endtime in dialogState.EndTime)
                        {
                            var endDateTime = new DateTime(
                                endDate.Year,
                                endDate.Month,
                                endDate.Day,
                                endtime.Hour,
                                endtime.Minute,
                                endtime.Second);
                            endDateTime = TimeZoneInfo.ConvertTimeToUtc(endDateTime, userState.GetUserTimeZone());
                            if (dialogState.EndDateTime == null || endDateTime >= dialogState.StartDateTime)
                            {
                                dialogState.EndDateTime = endDateTime;
                            }
                        }
                    }
                    else
                    {
                        dialogState.EndDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);
                        dialogState.EndDateTime = TimeZoneInfo.ConvertTimeToUtc(dialogState.EndDateTime.Value, userState.GetUserTimeZone());
                    }

                    var ts = dialogState.StartDateTime.Value.Subtract(dialogState.EndDateTime.Value).Duration();
                    dialogState.Duration = (int)ts.TotalSeconds;
                }

                if (dialogState.Duration <= 0 && dialogState.CreateHasDetail && dialogState.RecreateState != RecreateEventState.Time && dialogState.RecreateState != RecreateEventState.Duration)
                {
                    dialogState.Duration = 1800;
                }

                if (dialogState.Duration <= 0 && sc.Result != null)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);

                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                    if (dateTimeResolutions.First().Value != null)
                    {
                        int.TryParse(dateTimeResolutions.First().Value, out var duration);
                        dialogState.Duration = duration;
                    }
                }

                if (dialogState.Duration > 0)
                {
                    dialogState.EndDateTime = dialogState.StartDateTime.Value.AddSeconds(dialogState.Duration);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (sc.Result != null)
                {
                    var recreateState = sc.Result as RecreateEventState?;
                    switch (recreateState.Value)
                    {
                        case RecreateEventState.Cancel:
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded), cancellationToken);
                            await ClearAllState(sc.Context);
                            return await sc.EndDialogAsync(true, cancellationToken);
                        case RecreateEventState.Time:
                            dialogState.ClearTimes();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, skillOptions, cancellationToken: cancellationToken);
                        case RecreateEventState.Duration:
                            dialogState.ClearTimesExceptStartTime();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, skillOptions, cancellationToken: cancellationToken);
                        case RecreateEventState.Location:
                            dialogState.ClearLocation();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, skillOptions, cancellationToken: cancellationToken);
                        case RecreateEventState.Participants:
                            dialogState.ClearParticipants();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, skillOptions, cancellationToken: cancellationToken);
                        case RecreateEventState.Subject:
                            dialogState.ClearSubject();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, skillOptions, cancellationToken: cancellationToken);
                        case RecreateEventState.Content:
                            dialogState.ClearContent();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, skillOptions, cancellationToken: cancellationToken);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (CreateEventDialogState)sc.State.Dialog[CalendarStateKey];

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    await sc.Context.SendActivityAsync(dialogState.FindContactInfor.Contacts.GetRange(5, dialogState.FindContactInfor.Contacts.Count - 5).ToSpeechString(CommonStrings.And, li => li.DisplayName ?? li.Address));
                }

                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> InitCreateEventDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = new CreateEventDialogState();

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<CalendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions != null && skillOptions.SubFlowMode)
                {
                    dialogState = userState?.CacheModel != null ? new CreateEventDialogState(userState?.CacheModel) : dialogState;
                }

                var newState = await DigestCreateEventLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SaveCreateEventDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new CreateEventDialogState();

                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is CreateEventDialogState)
                    {
                        dialogState = (CreateEventDialogState)skillOptions.DialogState;
                    }
                    else
                    {
                        dialogState = skillOptions.DialogState != null ? new CreateEventDialogState(skillOptions.DialogState) : dialogState;
                    }
                }

                var userState = await CalendarStateAccessor.GetAsync(sc.Context);

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<CalendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                var newState = await DigestCreateEventLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState as CreateEventDialogState, true);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<CreateEventDialogState> DigestCreateEventLuisResult(DialogContext dc, CalendarLuis luisResult, General generalLuisResult, CreateEventDialogState state, bool isBeginDialog)
        {
            try
            {
                var userState = await CalendarStateAccessor.GetAsync(dc.Context);

                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

                if (!isBeginDialog)
                {
                    return state;
                }

                switch (intent)
                {
                    case CalendarLuis.Intent.FindMeetingRoom:
                    case CalendarLuis.Intent.CreateCalendarEntry:
                        {
                            state.CreateHasDetail = false;
                            if (entity.Subject != null)
                            {
                                state.CreateHasDetail = true;
                                state.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.personName != null)
                            {
                                state.CreateHasDetail = true;
                                state.FindContactInfor.ContactsNameList = GetAttendeesFromEntity(entity, luisResult.Text, state.FindContactInfor.ContactsNameList);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (date != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.StartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (date != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (time != null)
                                {
                                    state.CreateHasDetail = true;
                                    state.EndTime = time;
                                }
                            }

                            if (entity.Duration != null)
                            {
                                var duration = GetDurationFromEntity(entity, dc.Context.Activity.Locale);
                                if (duration != -1)
                                {
                                    state.CreateHasDetail = true;
                                    state.Duration = duration;
                                }
                            }

                            if (entity.MeetingRoom != null)
                            {
                                state.CreateHasDetail = true;
                                state.Location = GetMeetingRoomFromEntity(entity);
                            }

                            if (entity.Location != null)
                            {
                                state.CreateHasDetail = true;
                                state.Location = GetLocationFromEntity(entity);
                            }

                            break;
                        }

                }

                return state;
            }
            catch
            {
                await ClearAllState(dc.Context);
                await dc.CancelAllDialogsAsync();
                throw;
            }
        }
    }
}