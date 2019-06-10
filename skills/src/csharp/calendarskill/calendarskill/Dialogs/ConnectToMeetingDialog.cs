using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogModel;
using CalendarSkill.Responses.JoinEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using HtmlAgilityPack;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using static CalendarSkill.Models.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class ConnectToMeetingDialog : CalendarSkillDialogBase
    {
        public ConnectToMeetingDialog(
         BotSettings settings,
         BotServices services,
         ResponseManager responseManager,
         ConversationState conversationState,
         IServiceManager serviceManager,
         IBotTelemetryClient telemetryClient)
         : base(nameof(ConnectToMeetingDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var joinMeeting = new WaterfallStep[]
            {
                InitConnectToMeetingDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEventsSummary,
                AfterSelectEvent
            };

            var confirmNumber = new WaterfallStep[]
            {
                SaveConnectToMeetingDialogState,
                ConfirmNumber,
                AfterConfirmNumber
            };

            AddDialog(new CalendarWaterfallDialog(Actions.ConnectToMeeting, joinMeeting, CalendarStateAccessor) { TelemetryClient = telemetryClient });
            AddDialog(new CalendarWaterfallDialog(Actions.ConfirmNumber, confirmNumber, CalendarStateAccessor) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ConnectToMeeting;
        }

        private string GetDialInNumberFromMeeting(EventModel eventModel)
        {
            // Support teams and skype meeting.
            if (string.IsNullOrEmpty(eventModel.Content))
            {
                return null;
            }

            var body = eventModel.Content;
            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            var number = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'tel')]");
            if (number == null || string.IsNullOrEmpty(number.InnerText))
            {
                return null;
            }

            const string telToken = "&#43;";
            return number.InnerText.Replace(telToken, string.Empty);
        }

        private async Task<List<EventModel>> GetMeetingToJoin(WaterfallStepContext sc)
        {
            var userState = await CalendarStateAccessor.GetAsync(sc.Context);
            var dialogState = (ConnectToMettingDialogState)sc.State.Dialog[CalendarStateKey];

            var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);

            var eventList = await GetEventsByTime(new List<DateTime>() { DateTime.Today }, dialogState.StartTime, dialogState.EndDate, dialogState.EndTime, userState.GetUserTimeZone(), calendarService);
            var nextEventList = new List<EventModel>();
            foreach (var item in eventList)
            {
                var itemUserTimeZoneTime = TimeZoneInfo.ConvertTime(item.StartTime, TimeZoneInfo.Utc, userState.GetUserTimeZone());
                if (item.IsCancelled != true && IsValidJoinTime(userState.GetUserTimeZone(), item) && GetDialInNumberFromMeeting(item) != null)
                {
                    nextEventList.Add(item);
                }
            }

            return nextEventList;
        }

        private bool IsValidJoinTime(TimeZoneInfo userTimeZone, EventModel e)
        {
            var startTime = TimeZoneInfo.ConvertTime(e.StartTime, TimeZoneInfo.Utc, userTimeZone);
            var endTime = TimeZoneInfo.ConvertTime(e.EndTime, TimeZoneInfo.Utc, userTimeZone);
            var nowTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, userTimeZone);

            if (endTime >= nowTime || nowTime.AddHours(1) >= startTime)
            {
                return true;
            }

            return false;
        }

        private async Task<DialogTurnResult> ShowEventsSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var tokenResponse = sc.Result as TokenResponse;

                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (ConnectToMettingDialogState)sc.State.Dialog[CalendarStateKey];
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;

                var options = sc.Options as ShowMeetingsDialogOptions;
                if (dialogState.SummaryEvents == null)
                {
                    // this will lead to error when test
                    if (string.IsNullOrEmpty(userState.APIToken))
                    {
                        dialogState.Clear();
                        return await sc.EndDialogAsync(true);
                    }

                    var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);

                    dialogState.SummaryEvents = await GetMeetingToJoin(sc);
                }

                if (dialogState.SummaryEvents.Count == 0)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.MeetingNotFound));
                    dialogState.Clear();
                    return await sc.EndDialogAsync(true);
                }
                else if (dialogState.SummaryEvents.Count == 1)
                {
                    dialogState.ConfirmedMeeting.Add(dialogState.SummaryEvents.First());
                    skillOptions.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }

                // Multiple events
                var firstEvent = GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState).First();

                var responseParams = new StringDictionary()
                {
                    { "EventName1", firstEvent.Title },
                    { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(firstEvent.StartTime, userState.GetUserTimeZone()), firstEvent.IsAllDay == true) },
                    { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(firstEvent.Attendees, 1) }
                };

                var reply = await GetGeneralMeetingListResponseAsync(sc, CalendarCommonStrings.MeetingsToJoin, GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState), JoinEventResponses.SelectMeeting, responseParams);

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = reply });
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

        private async Task<DialogTurnResult> AfterSelectEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (ConnectToMettingDialogState)sc.State.Dialog[CalendarStateKey];
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;

                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (topIntent == null)
                {
                    dialogState.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if (generalTopIntent == General.Intent.ShowNext && dialogState.SummaryEvents != null)
                {
                    if ((dialogState.ShowEventIndex + 1) * userState.PageSize < dialogState.SummaryEvents.Count)
                    {
                        dialogState.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoMoreEvent));
                    }

                    skillOptions.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, sc.Options);
                }
                else if (generalTopIntent == General.Intent.ShowPrevious && dialogState.SummaryEvents != null)
                {
                    if (dialogState.ShowEventIndex > 0)
                    {
                        dialogState.ShowEventIndex--;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoPreviousEvent));
                    }

                    skillOptions.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, sc.Options);
                }

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    dialogState.Clear();
                    return await sc.CancelAllDialogsAsync();
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    var currentList = GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState);
                    dialogState.ConfirmedMeeting.Add(currentList.First());
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }
                else if (dialogState.SummaryEvents.Count == 1)
                {
                    dialogState.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if (dialogState.SummaryEvents.Count > 1)
                {
                    var filteredMeetingList = new List<EventModel>();
                    var showMeetingReason = ShowMeetingReason.FirstShowOverview;
                    string filterKeyWord = null;

                    // filter meetings with number
                    if (luisResult.Entities.ordinal != null)
                    {
                        var value = luisResult.Entities.ordinal[0];
                        var num = int.Parse(value.ToString());
                        var currentList = GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState);
                        if (num > 0 && num <= currentList.Count)
                        {
                            filteredMeetingList.Add(currentList[num - 1]);
                        }
                    }

                    if (filteredMeetingList.Count <= 0 && luisResult.Entities.number != null && (luisResult.Entities.ordinal == null || luisResult.Entities.ordinal.Length == 0))
                    {
                        var value = luisResult.Entities.number[0];
                        var num = int.Parse(value.ToString());
                        var currentList = GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState);
                        if (num > 0 && num <= currentList.Count)
                        {
                            filteredMeetingList.Add(currentList[num - 1]);
                        }
                    }

                    // filter meetings with start time
                    var timeResult = RecognizeDateTime(userInput, sc.Context.Activity.Locale ?? English, false);
                    if (filteredMeetingList.Count <= 0 && timeResult != null)
                    {
                        foreach (var result in timeResult)
                        {
                            var dateTimeConvertTypeString = result.Timex;
                            var dateTimeConvertType = new TimexProperty(dateTimeConvertTypeString);
                            if (result.Value != null || (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Time) || dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime)))
                            {
                                var dateTime = DateTime.Parse(result.Value);

                                if (dateTime != null)
                                {
                                    var utcStartTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, userState.GetUserTimeZone());
                                    foreach (var meeting in GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState))
                                    {
                                        if (meeting.StartTime.TimeOfDay == utcStartTime.TimeOfDay)
                                        {
                                            showMeetingReason = ShowMeetingReason.ShowFilteredByTimeMeetings;
                                            filterKeyWord = string.Format("H:mm", dateTime);
                                            filteredMeetingList.Add(meeting);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // filter meetings with subject
                    if (filteredMeetingList.Count <= 0)
                    {
                        var subject = userInput;
                        if (luisResult.Entities.Subject != null)
                        {
                            subject = GetSubjectFromEntity(luisResult.Entities);
                        }

                        foreach (var meeting in GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState))
                        {
                            if (meeting.Title.ToLower().Contains(subject.ToLower()))
                            {
                                showMeetingReason = ShowMeetingReason.ShowFilteredByTitleMeetings;
                                filterKeyWord = subject;
                                filteredMeetingList.Add(meeting);
                            }
                        }
                    }

                    // filter meetings with contact name
                    if (filteredMeetingList.Count <= 0)
                    {
                        var contactNameList = new List<string>() { userInput };
                        if (luisResult.Entities.personName != null)
                        {
                            contactNameList = GetAttendeesFromEntity(luisResult.Entities, userInput);
                        }

                        foreach (var meeting in GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState))
                        {
                            var containsAllContacts = true;
                            foreach (var contactName in contactNameList)
                            {
                                if (!meeting.ContainsAttendee(contactName))
                                {
                                    containsAllContacts = false;
                                    break;
                                }
                            }

                            if (containsAllContacts)
                            {
                                showMeetingReason = ShowMeetingReason.ShowFilteredByParticipantNameMeetings;
                                filterKeyWord = string.Join(", ", contactNameList);
                                filteredMeetingList.Add(meeting);
                            }
                        }
                    }

                    if (filteredMeetingList.Count == 1)
                    {
                        skillOptions.DialogState = dialogState;
                        return await sc.BeginDialogAsync(Actions.ConfirmNumber, sc.Options);
                    }
                    else if (filteredMeetingList.Count > 1)
                    {
                        dialogState.SummaryEvents = filteredMeetingList;
                        skillOptions.DialogState = dialogState;
                        return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, new ShowMeetingsDialogOptions(showMeetingReason, sc.Options));
                    }
                }

                if (dialogState.ConfirmedMeeting != null && dialogState.ConfirmedMeeting.Count > 0)
                {
                    skillOptions.DialogState = dialogState;
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }
                else
                {
                    dialogState.Clear();
                    return await sc.CancelAllDialogsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmNumber(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var dialogState = (ConnectToMettingDialogState)sc.State.Dialog[CalendarStateKey];

            var selectedEvent = dialogState.ConfirmedMeeting.First();
            var phoneNumber = GetDialInNumberFromMeeting(selectedEvent);
            var responseParams = new StringDictionary()
            {
                { "PhoneNumber", phoneNumber },
            };
            return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions() { Prompt = ResponseManager.GetResponse(JoinEventResponses.ConfirmPhoneNumber, responseParams) });
        }

        private async Task<DialogTurnResult> AfterConfirmNumber(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var dialogState = (ConnectToMettingDialogState)sc.State.Dialog[CalendarStateKey];

            if (sc.Result is bool)
            {
                if ((bool)sc.Result)
                {
                    var selectedEvent = dialogState.ConfirmedMeeting.First();
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.JoinMeeting));
                    var replyEvent = sc.Context.Activity.CreateReply();
                    replyEvent.Type = ActivityTypes.Event;
                    replyEvent.Name = "JoinEvent.DialInNumber";
                    replyEvent.Value = GetDialInNumberFromMeeting(selectedEvent);
                    await sc.Context.SendActivityAsync(replyEvent, cancellationToken);
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.NotJoinMeeting));
                }
            }

            dialogState.SummaryEvents.Clear();

            return await sc.EndDialogAsync();
        }

        private async Task<DialogTurnResult> InitConnectToMeetingDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = new ConnectToMettingDialogState();

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
                    dialogState = userState?.CacheModel != null ? new ConnectToMettingDialogState(userState?.CacheModel) : dialogState;
                }

                var newState = await DigestConnectToMeetingLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState, true);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> SaveConnectToMeetingDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new ConnectToMettingDialogState();

                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is ConnectToMettingDialogState)
                    {
                        dialogState = (ConnectToMettingDialogState)skillOptions.DialogState;
                    }
                    else
                    {
                        dialogState = skillOptions.DialogState != null ? new ConnectToMettingDialogState(skillOptions.DialogState) : dialogState;
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

                var newState = await DigestConnectToMeetingLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState as ConnectToMettingDialogState, false);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<ConnectToMettingDialogState> DigestConnectToMeetingLuisResult(DialogContext dc, CalendarLuis luisResult, General generalLuisResult, ConnectToMettingDialogState state, bool isBeginDialog)
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
                    case CalendarLuis.Intent.ConnectToMeeting:
                        {
                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.StartDate = date;
                                }
                            }

                            if (entity.ToDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (date != null)
                                {
                                    state.EndDate = date;
                                }
                            }

                            if (entity.FromTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (time != null)
                                {
                                    state.StartTime = time;
                                }

                                time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.ToTime != null)
                            {
                                var timeString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.ToTime[0]);
                                var time = GetTimeFromDateTimeString(timeString, dc.Context.Activity.Locale, userState.GetUserTimeZone());
                                if (time != null)
                                {
                                    state.EndTime = time;
                                }
                            }

                            if (entity.OrderReference != null)
                            {
                                state.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.Subject != null)
                            {
                                state.Title = entity._instance.Subject[0].Text;
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