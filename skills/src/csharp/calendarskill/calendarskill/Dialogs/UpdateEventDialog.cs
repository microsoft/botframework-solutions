using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogModel;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CalendarSkill.Dialogs
{
    public class UpdateEventDialog : CalendarSkillDialogBase
    {
        public UpdateEventDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(UpdateEventDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;
            var updateEvent = new WaterfallStep[]
            {
                InitUpdateEventDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                FromTokenToStartTime,
                FromEventsToNewDate,
                ConfirmBeforeUpdate,
                UpdateEventTime,
            };

            var updateStartTime = new WaterfallStep[]
            {
                SaveUpdateEventDialogState,
                UpdateStartTime,
                AfterUpdateStartTime,
            };

            var updateNewStartTime = new WaterfallStep[]
            {
                SaveUpdateEventDialogState,
                GetNewEventTime,
                AfterGetNewEventTime,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new CalendarWaterfallDialog(Actions.UpdateEventTime, updateEvent, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.UpdateStartTime, updateStartTime, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.UpdateNewStartTime, updateNewStartTime, CalendarStateAccessor) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.UpdateEventTime;
        }

        public async Task<DialogTurnResult> FromEventsToNewDate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (sc.Result != null && sc.Result is FoundChoice && dialogState.Events.Count > 1)
                {
                    var events = dialogState.Events;
                    dialogState.Events = new List<EventModel>
                {
                    events[(sc.Result as FoundChoice).Index],
                };
                }

                var origin = dialogState.Events[0];
                if (!origin.IsOrganizer)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(UpdateEventResponses.NotEventOrganizer));
                    await ClearAllState(sc.Context);
                    return await sc.EndDialogAsync(true);
                }
                else if (dialogState.NewStartDateTime == null)
                {
                    skillOptions.DialogState = dialogState;
                    return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound, skillOptions));
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

        public async Task<DialogTurnResult> ConfirmBeforeUpdate(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result is CalendarSkillDialogOptions skillOptions)
                {
                    sc.State.Dialog[CalendarStateKey] = skillOptions.DialogState;
                }

                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                var newStartTime = (DateTime)dialogState.NewStartDateTime;
                var origin = dialogState.Events[0];
                var last = origin.EndTime - origin.StartTime;
                origin.StartTime = newStartTime;
                origin.EndTime = (newStartTime + last).AddSeconds(1);

                var replyMessage = await GetDetailMeetingResponseAsync(sc, origin, UpdateEventResponses.ConfirmUpdate);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.ConfirmUpdateFailed),
                });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> UpdateEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    var newStartTime = (DateTime)dialogState.NewStartDateTime;
                    var origin = dialogState.Events[0];
                    var updateEvent = new EventModel(origin.Source);
                    var last = origin.EndTime - origin.StartTime;
                    updateEvent.StartTime = newStartTime;
                    updateEvent.EndTime = (newStartTime + last).AddSeconds(1);
                    updateEvent.TimeZone = TimeZoneInfo.Utc;
                    updateEvent.Id = origin.Id;

                    if (!string.IsNullOrEmpty(dialogState.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                    {
                        updateEvent.Id = origin.RecurringId;
                    }

                    var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);
                    var newEvent = await calendarService.UpdateEventById(updateEvent);

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, UpdateEventResponses.EventUpdated);

                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(CalendarSharedResponses.ActionEnded));
                }

                if (!skillOptions.SubFlowMode)
                {
                    await ClearAllState(sc.Context);
                }

                return await sc.EndDialogAsync(true);
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

        public async Task<DialogTurnResult> GetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.NewStartDate.Any() || dialogState.NewStartTime.Any() || dialogState.MoveTimeSpan != 0)
                {
                    return await sc.ContinueDialogAsync();
                }

                return await sc.PromptAsync(Actions.TimePrompt, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(UpdateEventResponses.NoNewTime),
                    RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.NoNewTimeRetry)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterGetNewEventTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.NewStartDate.Any() || dialogState.NewStartTime.Any() || dialogState.MoveTimeSpan != 0)
                {
                    var originalEvent = dialogState.Events[0];
                    var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(originalEvent.StartTime, userState.GetUserTimeZone());
                    var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userState.GetUserTimeZone());

                    if (dialogState.NewStartDate.Any() || dialogState.NewStartTime.Any())
                    {
                        var newStartDate = dialogState.NewStartDate.Any() ?
                            dialogState.NewStartDate.Last() :
                            originalStartDateTime;

                        var newStartTime = new List<DateTime>();
                        if (dialogState.NewStartTime.Any())
                        {
                            foreach (var time in dialogState.NewStartTime)
                            {
                                var newStartDateTime = new DateTime(
                                    newStartDate.Year,
                                    newStartDate.Month,
                                    newStartDate.Day,
                                    time.Hour,
                                    time.Minute,
                                    time.Second);

                                if (dialogState.NewStartDateTime == null)
                                {
                                    dialogState.NewStartDateTime = newStartDateTime;
                                }

                                if (newStartDateTime >= userNow)
                                {
                                    dialogState.NewStartDateTime = newStartDateTime;
                                    break;
                                }
                            }
                        }
                    }
                    else if (dialogState.MoveTimeSpan != 0)
                    {
                        dialogState.NewStartDateTime = originalStartDateTime.AddSeconds(dialogState.MoveTimeSpan);
                    }
                    else
                    {
                        skillOptions.DialogState = dialogState;
                        return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound, skillOptions));
                    }

                    dialogState.NewStartDateTime = TimeZoneInfo.ConvertTimeToUtc(dialogState.NewStartDateTime.Value, userState.GetUserTimeZone());

                    skillOptions.DialogState = dialogState;
                    return await sc.EndDialogAsync(skillOptions);
                }
                else if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;

                    DateTime? newStartTime = null;

                    foreach (var resolution in dateTimeResolutions)
                    {
                        var utcNow = DateTime.UtcNow;
                        var dateTimeConvertTypeString = resolution.Timex;
                        var dateTimeConvertType = new TimexProperty(dateTimeConvertTypeString);
                        var dateTimeValue = DateTime.Parse(resolution.Value);
                        if (dateTimeValue == null)
                        {
                            continue;
                        }

                        var isRelativeTime = IsRelativeTime(sc.Context.Activity.Text, resolution.Value, dateTimeConvertTypeString);
                        if (isRelativeTime)
                        {
                            dateTimeValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Local);
                        }

                        dateTimeValue = isRelativeTime ? TimeZoneInfo.ConvertTime(dateTimeValue, TimeZoneInfo.Local, userState.GetUserTimeZone()) : dateTimeValue;
                        var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(dialogState.Events[0].StartTime, userState.GetUserTimeZone());
                        if (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Date) && !dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime))
                        {
                            dateTimeValue = new DateTime(
                                dateTimeValue.Year,
                                dateTimeValue.Month,
                                dateTimeValue.Day,
                                originalStartDateTime.Hour,
                                originalStartDateTime.Minute,
                                originalStartDateTime.Second);
                        }
                        else if (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Time) && !dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime))
                        {
                            dateTimeValue = new DateTime(
                                originalStartDateTime.Year,
                                originalStartDateTime.Month,
                                originalStartDateTime.Day,
                                dateTimeValue.Hour,
                                dateTimeValue.Minute,
                                dateTimeValue.Second);
                        }

                        dateTimeValue = TimeZoneInfo.ConvertTimeToUtc(dateTimeValue, userState.GetUserTimeZone());

                        if (newStartTime == null)
                        {
                            newStartTime = dateTimeValue;
                        }

                        if (dateTimeValue >= utcNow)
                        {
                            newStartTime = dateTimeValue;
                            break;
                        }
                    }

                    if (newStartTime != null)
                    {
                        dialogState.NewStartDateTime = newStartTime;
                        skillOptions.DialogState = dialogState;

                        return await sc.EndDialogAsync(skillOptions);
                    }
                    else
                    {
                        skillOptions.DialogState = dialogState;
                        return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime, skillOptions));
                    }
                }
                else
                {
                    skillOptions.DialogState = dialogState;
                    return await sc.BeginDialogAsync(Actions.UpdateNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime, skillOptions));
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> FromTokenToStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (string.IsNullOrEmpty(userState.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);
                skillOptions.DialogState = dialogState;
                return await sc.BeginDialogAsync(Actions.UpdateStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound, skillOptions));
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

        public async Task<DialogTurnResult> UpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (dialogState.Events.Count > 0)
                {
                    return await sc.NextAsync();
                }

                var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);

                if (dialogState.OriginalStartDate.Any() || dialogState.OriginalStartTime.Any())
                {
                    dialogState.Events = await GetEventsByTime(dialogState.OriginalStartDate, dialogState.OriginalStartTime, dialogState.OriginalEndDate, dialogState.OriginalEndTime, userState.GetUserTimeZone(), calendarService);
                    dialogState.OriginalStartDate = new List<DateTime>();
                    dialogState.OriginalStartTime = new List<DateTime>();
                    dialogState.OriginalEndDate = new List<DateTime>();
                    dialogState.OriginalEndTime = new List<DateTime>();
                    if (dialogState.Events.Count > 0)
                    {
                        return await sc.NextAsync();
                    }
                }

                if (dialogState.Title != null)
                {
                    dialogState.Events = await calendarService.GetEventsByTitle(dialogState.Title);
                    dialogState.Title = null;
                    if (dialogState.Events.Count > 0)
                    {
                        return await sc.NextAsync();
                    }
                }

                return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, userState.GetUserTimeZone())
                {
                    Prompt = ResponseManager.GetResponse(UpdateEventResponses.NoUpdateStartTime),
                    RetryPrompt = ResponseManager.GetResponse(UpdateEventResponses.EventWithStartTimeNotFound)
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateStartTime(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (UpdateEventDialogState)sc.State.Dialog[CalendarStateKey];

                if (sc.Result != null)
                {
                    dialogState.Events = sc.Result as List<EventModel>;
                }

                if (dialogState.Events.Count == 0)
                {
                    // should not doto this part. add log here for safe
                    await HandleDialogExceptions(sc, new Exception("Unexpect zero events count"));
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
                else
                if (dialogState.Events.Count > 1)
                {
                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>(),
                    };

                    for (var i = 0; i < dialogState.Events.Count; i++)
                    {
                        var item = dialogState.Events[i];
                        var choice = new Choice()
                        {
                            Value = string.Empty,
                            Synonyms = new List<string> { (i + 1).ToString(), item.Title },
                        };
                        options.Choices.Add(choice);
                    }

                    var prompt = await GetGeneralMeetingListResponseAsync(sc, CalendarCommonStrings.MeetingsToChoose, dialogState.Events, UpdateEventResponses.MultipleEventsStartAtSameTime, null);

                    options.Prompt = prompt;

                    return await sc.PromptAsync(Actions.EventChoice, options);
                }
                else
                {
                    skillOptions.DialogState = dialogState;
                    return await sc.EndDialogAsync(skillOptions);
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

        private async Task<DialogTurnResult> InitUpdateEventDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = new UpdateEventDialogState();

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<calendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions != null && skillOptions.SubFlowMode)
                {
                    dialogState = userState?.CacheModel != null ? new UpdateEventDialogState(userState?.CacheModel) : dialogState;
                }

                var newState = await DigestUpdateEventLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SaveUpdateEventDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new UpdateEventDialogState();

                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is UpdateEventDialogState)
                    {
                        dialogState = (UpdateEventDialogState)skillOptions.DialogState;
                    }
                    else
                    {
                        dialogState = skillOptions.DialogState != null ? new UpdateEventDialogState(skillOptions.DialogState) : dialogState;
                    }
                }

                var userState = await CalendarStateAccessor.GetAsync(sc.Context);

                var locale = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var localeConfig = Services.CognitiveModelSets[locale];

                // Update state with email luis result and entities --- todo: use luis result in adaptive dialog
                var luisResult = await localeConfig.LuisServices["calendar"].RecognizeAsync<calendarLuis>(sc.Context);
                userState.LuisResult = luisResult;
                localeConfig.LuisServices.TryGetValue("general", out var luisService);
                var generalLuisResult = await luisService.RecognizeAsync<General>(sc.Context);
                userState.GeneralLuisResult = generalLuisResult;

                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                var newState = await DigestUpdateEventLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState as UpdateEventDialogState, false);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<UpdateEventDialogState> DigestUpdateEventLuisResult(DialogContext dc, calendarLuis luisResult, General generalLuisResult, UpdateEventDialogState state, bool isBeginDialog)
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
                    case calendarLuis.Intent.ChangeCalendarEntry:
                        {
                            if (entity.Subject != null)
                            {
                                state.Title = GetSubjectFromEntity(entity);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (date != null)
                                {
                                    state.OriginalStartDate = date;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (date != null)
                                {
                                    state.OriginalEndDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.NewStartDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.OriginalStartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.OriginalEndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.NewStartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.NewEndTime = time;
                                }
                            }

                            if (entity.MoveEarlierTimeSpan != null)
                            {
                                state.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveEarlierTimeSpan[0], dc.Context.Activity.Locale, false);
                            }

                            if (entity.MoveLaterTimeSpan != null)
                            {
                                state.MoveTimeSpan = GetMoveTimeSpanFromEntity(entity.MoveLaterTimeSpan[0], dc.Context.Activity.Locale, true);
                            }

                            if (entity.datetime != null)
                            {
                                var match = entity._instance.datetime.ToList().Find(w => w.Text.ToLower() == CalendarCommonStrings.DailyToken
                                || w.Text.ToLower() == CalendarCommonStrings.WeeklyToken
                                || w.Text.ToLower() == CalendarCommonStrings.MonthlyToken);
                                if (match != null)
                                {
                                    state.RecurrencePattern = match.Text.ToLower();
                                }
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