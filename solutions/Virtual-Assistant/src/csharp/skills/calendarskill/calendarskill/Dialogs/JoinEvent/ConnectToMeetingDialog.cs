using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.JoinEvent.Resources;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.DialogOptions;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.Util;
using HtmlAgilityPack;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;
using static CalendarSkill.Dialogs.Shared.DialogOptions.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs.JoinEvent
{
    public class ConnectToMeetingDialog : CalendarSkillDialog
    {
        public ConnectToMeetingDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(ConnectToMeetingDialog), services, responseManager, accessor, serviceManager, telemetryClient)
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

            var eventList = await GetEventsByTime(new List<DateTime>() { DateTime.Today }, state.StartTime, state.EndDate, state.EndTime, state.GetUserTimeZone(), calendarService);
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
                if (state.SummaryEvents == null)
                {
                    // this will lead to error when test
                    if (string.IsNullOrEmpty(state.APIToken))
                    {
                        state.Clear();
                        return await sc.EndDialogAsync(true);
                    }

                    var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                    state.SummaryEvents = await GetMeetingToJoin(sc);
                }

                if (state.SummaryEvents.Count == 0)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.MeetingNotFound));
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }
                else if (state.SummaryEvents.Count == 1)
                {
                    state.ConfirmedMeeting.Add(state.SummaryEvents.First());
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }

                // Multiple events
                var firstEvent = GetCurrentPageMeetings(state.SummaryEvents, state).First();

                var responseParams = new StringDictionary()
                {
                    { "EventName1", firstEvent.Title },
                    { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(firstEvent.StartTime, state.GetUserTimeZone()), firstEvent.IsAllDay == true) },
                    { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(firstEvent.Attendees) }
                };

                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(JoinEventResponses.SelectMeeting, responseParams));
                await ShowMeetingList(sc, GetCurrentPageMeetings(state.SummaryEvents, state), false);

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions());
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

                if (generalTopIntent == General.Intent.Next && state.SummaryEvents != null)
                {
                    if ((state.ShowEventIndex + 1) * state.PageSize < state.SummaryEvents.Count)
                    {
                        state.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoMoreEvent));
                    }

                    return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, sc.Options);
                }
                else if (generalTopIntent == General.Intent.Previous && state.SummaryEvents != null)
                {
                    if (state.ShowEventIndex > 0)
                    {
                        state.ShowEventIndex--;
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
                    var currentList = GetCurrentPageMeetings(state.SummaryEvents, state);
                    state.ConfirmedMeeting.Add(currentList.First());
                    return await sc.ReplaceDialogAsync(Actions.ConfirmNumber, sc.Options);
                }
                else if (state.SummaryEvents.Count == 1)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if (state.SummaryEvents.Count > 1)
                {
                    var filteredMeetingList = new List<EventModel>();

                    // filter meetings with number
                    if (luisResult.Entities.ordinal != null)
                    {
                        var value = luisResult.Entities.ordinal[0];
                        var num = int.Parse(value.ToString());
                        var currentList = GetCurrentPageMeetings(state.SummaryEvents, state);
                        if (num > 0 && num <= currentList.Count)
                        {
                            filteredMeetingList.Add(currentList[num - 1]);
                        }
                    }

                    if (filteredMeetingList.Count <= 0 && luisResult.Entities.number != null && (luisResult.Entities.ordinal == null || luisResult.Entities.ordinal.Length == 0))
                    {
                        var value = luisResult.Entities.number[0];
                        var num = int.Parse(value.ToString());
                        var currentList = GetCurrentPageMeetings(state.SummaryEvents, state);
                        if (num > 0 && num <= currentList.Count)
                        {
                            filteredMeetingList.Add(currentList[num - 1]);
                        }
                    }

                    // filter meetings with start time
                    var timeResult = RecognizeDateTime(userInput, sc.Context.Activity.Locale ?? English);
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
                                    foreach (var meeting in GetCurrentPageMeetings(state.SummaryEvents, state))
                                    {
                                        if (meeting.StartTime.TimeOfDay == utcStartTime.TimeOfDay)
                                        {
                                            filteredMeetingList.Add(meeting);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // filter meetings with subject
                    var subject = userInput;
                    if (filteredMeetingList.Count <= 0 && luisResult.Entities.Subject != null)
                    {
                        subject = GetSubjectFromEntity(luisResult.Entities);
                    }

                    foreach (var meeting in GetCurrentPageMeetings(state.SummaryEvents, state))
                    {
                        if (meeting.Title.ToLower().Contains(subject.ToLower()))
                        {
                            filteredMeetingList.Add(meeting);
                        }
                    }

                    // filter meetings with contact name
                    var contactNameList = new List<string>() { userInput };
                    if (filteredMeetingList.Count <= 0 && luisResult.Entities.personName != null)
                    {
                        contactNameList = GetAttendeesFromEntity(luisResult.Entities, userInput);
                    }

                    foreach (var meeting in GetCurrentPageMeetings(state.SummaryEvents, state))
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
                            filteredMeetingList.Add(meeting);
                        }
                    }

                    if (filteredMeetingList.Count == 1)
                    {
                        state.ConfirmedMeeting = filteredMeetingList;
                        return await sc.BeginDialogAsync(Actions.ConfirmNumber, sc.Options);
                    }
                    else if (filteredMeetingList.Count > 1)
                    {
                        state.SummaryEvents = filteredMeetingList;
                        return await sc.ReplaceDialogAsync(Actions.ConnectToMeeting, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowFilteredMeetings, sc.Options));
                    }
                }

                if (state.ConfirmedMeeting != null && state.ConfirmedMeeting.Count > 0)
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

            var selectedEvent = state.ConfirmedMeeting.First();
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
                    var selectedEvent = state.ConfirmedMeeting.First();
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

            state.SummaryEvents.Clear();

            return await sc.EndDialogAsync();
        }

        private List<EventModel> GetCurrentPageMeetings(List<EventModel> allMeetings, CalendarSkillState state)
        {
            return allMeetings.GetRange(state.ShowEventIndex * state.PageSize, Math.Min(state.PageSize, allMeetings.Count - (state.ShowEventIndex * state.PageSize)));
        }
    }
}