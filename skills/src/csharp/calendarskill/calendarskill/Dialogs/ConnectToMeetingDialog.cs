using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
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
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using static CalendarSkill.Models.DialogOptions.ShowMeetingsDialogOptions;
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
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ConnectToMeetingDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var joinMeeting = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowEventsSummary,
                AfterSelectEvent
            };

            var confirmNumber = new WaterfallStep[]
            {
                ConfirmNumber,
                AfterConfirmNumber
            };

            AddDialog(new WaterfallDialog(Actions.ConnectToMeeting, joinMeeting) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConfirmNumber, confirmNumber) { TelemetryClient = telemetryClient });

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
            var state = await Accessor.GetAsync(sc.Context);
            var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

            var eventList = await GetEventsByTime(new List<DateTime>() { DateTime.Today }, state.MeetingInfor.StartTime, state.MeetingInfor.EndDate, state.MeetingInfor.EndTime, state.GetUserTimeZone(), calendarService);
            var nextEventList = new List<EventModel>();
            foreach (var item in eventList)
            {
                var itemUserTimeZoneTime = TimeZoneInfo.ConvertTime(item.StartTime, TimeZoneInfo.Utc, state.GetUserTimeZone());
                if (item.IsCancelled != true && IsValidJoinTime(state.GetUserTimeZone(), item) && GetDialInNumberFromMeeting(item) != null)
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

                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as ShowMeetingsDialogOptions;
                if (state.ShowMeetingInfor.ShowingMeetings == null)
                {
                    // this will lead to error when test
                    if (string.IsNullOrEmpty(state.APIToken))
                    {
                        state.Clear();
                        return await sc.EndDialogAsync(true);
                    }

                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                    state.ShowMeetingInfor.ShowingMeetings = await GetMeetingToJoin(sc);
                }

                if (state.ShowMeetingInfor.ShowingMeetings.Count == 0)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.MeetingNotFound));
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }
                else if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                {
                    state.ShowMeetingInfor.FocusedEvents.Add(state.ShowMeetingInfor.ShowingMeetings.First());
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }

                // Multiple events
                var firstEvent = GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state).First();

                var responseParams = new StringDictionary()
                {
                    { "EventName1", firstEvent.Title },
                    { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(firstEvent.StartTime, state.GetUserTimeZone()), firstEvent.IsAllDay == true) },
                    { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(firstEvent.Attendees, 1) }
                };

                var reply = await GetGeneralMeetingListResponseAsync(sc, CalendarCommonStrings.MeetingsToJoin, GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state), JoinEventResponses.SelectMeeting, responseParams);

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
                var state = await Accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (topIntent == null)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if (generalTopIntent == General.Intent.ShowNext && state.ShowMeetingInfor.ShowingMeetings != null)
                {
                    if ((state.ShowMeetingInfor.ShowEventIndex + 1) * state.PageSize < state.ShowMeetingInfor.ShowingMeetings.Count)
                    {
                        state.ShowMeetingInfor.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoMoreEvent));
                    }

                    return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, sc.Options);
                }
                else if (generalTopIntent == General.Intent.ShowPrevious && state.ShowMeetingInfor.ShowingMeetings != null)
                {
                    if (state.ShowMeetingInfor.ShowEventIndex > 0)
                    {
                        state.ShowMeetingInfor.ShowEventIndex--;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoPreviousEvent));
                    }

                    return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, sc.Options);
                }

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    var currentList = GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state);
                    state.ShowMeetingInfor.FocusedEvents.Add(currentList.First());
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }
                else if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if (state.ShowMeetingInfor.ShowingMeetings.Count > 1)
                {
                    var filteredMeetingList = new List<EventModel>();
                    var showMeetingReason = ShowMeetingReason.FirstShowOverview;
                    string filterKeyWord = null;

                    // filter meetings with number
                    if (luisResult.Entities.ordinal != null)
                    {
                        var value = luisResult.Entities.ordinal[0];
                        var num = int.Parse(value.ToString());
                        var currentList = GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state);
                        if (num > 0 && num <= currentList.Count)
                        {
                            filteredMeetingList.Add(currentList[num - 1]);
                        }
                    }

                    if (filteredMeetingList.Count <= 0 && generalLuisResult.Entities.number != null && (luisResult.Entities.ordinal == null || luisResult.Entities.ordinal.Length == 0))
                    {
                        var value = generalLuisResult.Entities.number[0];
                        var num = int.Parse(value.ToString());
                        var currentList = GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state);
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
                                    var utcStartTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, state.GetUserTimeZone());
                                    foreach (var meeting in GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state))
                                    {
                                        if (meeting.StartTime.TimeOfDay == utcStartTime.TimeOfDay)
                                        {
                                            showMeetingReason = ShowMeetingReason.ShowFilteredByTimeMeetings;
                                            filterKeyWord = dateTime.ToString("H:mm");
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

                        foreach (var meeting in GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state))
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

                        foreach (var meeting in GetCurrentPageMeetings(state.ShowMeetingInfor.ShowingMeetings, state))
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
                        state.ShowMeetingInfor.FocusedEvents = filteredMeetingList;
                        return await sc.BeginDialogAsync(Actions.ConfirmNumber, sc.Options);
                    }
                    else if (filteredMeetingList.Count > 1)
                    {
                        state.ShowMeetingInfor.ShowingMeetings = filteredMeetingList;
                        state.ShowMeetingInfor.FilterMeetingKeyWord = filterKeyWord;
                        return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, new ShowMeetingsDialogOptions(showMeetingReason, sc.Options));
                    }
                }

                if (state.ShowMeetingInfor.FocusedEvents != null && state.ShowMeetingInfor.FocusedEvents.Count > 0)
                {
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }
                else
                {
                    state.Clear();
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
            var state = await Accessor.GetAsync(sc.Context);

            var selectedEvent = state.ShowMeetingInfor.FocusedEvents.First();
            var phoneNumber = GetDialInNumberFromMeeting(selectedEvent);
            var responseParams = new StringDictionary()
            {
                { "PhoneNumber", phoneNumber },
            };
            return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions() { Prompt = ResponseManager.GetResponse(JoinEventResponses.ConfirmPhoneNumber, responseParams) });
        }

        private async Task<DialogTurnResult> AfterConfirmNumber(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context);
            if (sc.Result is bool)
            {
                if ((bool)sc.Result)
                {
                    var selectedEvent = state.ShowMeetingInfor.FocusedEvents.First();
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

            state.ShowMeetingInfor.ShowingMeetings.Clear();

            return await sc.EndDialogAsync();
        }

        private List<EventModel> GetCurrentPageMeetings(List<EventModel> allMeetings, CalendarSkillState state)
        {
            return allMeetings.GetRange(state.ShowMeetingInfor.ShowEventIndex * state.PageSize, Math.Min(state.PageSize, allMeetings.Count - (state.ShowMeetingInfor.ShowEventIndex * state.PageSize)));
        }
    }
}