using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.ChangeEventStatus;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.DialogOptions;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.Dialogs.UpdateEvent;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using static CalendarSkill.Dialogs.Shared.DialogOptions.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs.Summary
{
    public class SummaryDialog : CalendarSkillDialog
    {
        public SummaryDialog(
            SkillConfigurationBase services,
            ResponseManager responseManager,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SummaryDialog), services, responseManager, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var initStep = new WaterfallStep[]
            {
                Init,
            };

            var showNext = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowNextEvent,
            };

            var showSummary = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ShowEventsSummary,
                CallReadEventDialog,
                AskForShowOverview,
                AfterAskForShowOverview
            };

            var readEvent = new WaterfallStep[]
            {
                ReadEvent,
                AfterReadOutEvent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.GetEventsInit, initStep) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowNextEvent, showNext) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowEventsSummary, showSummary) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Read, readEvent) { TelemetryClient = telemetryClient });
            AddDialog(new UpdateEventDialog(services, responseManager, accessor, serviceManager, telemetryClient));
            AddDialog(new ChangeEventStatusDialog(services, responseManager, accessor, serviceManager, telemetryClient));

            // Set starting dialog for component
            InitialDialogId = Actions.GetEventsInit;
        }

        public async Task<DialogTurnResult> Init(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.OrderReference != null && state.OrderReference == "next")
                {
                    return await sc.BeginDialogAsync(Actions.ShowNextEvent, options: sc.Options);
                }

                return await sc.BeginDialogAsync(Actions.ShowEventsSummary, options: sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ShowEventsSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                    var searchDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());

                    if (state.StartDate.Any())
                    {
                        searchDate = state.StartDate.Last();
                    }

                    var results = await GetEventsByTime(new List<DateTime>() { searchDate }, state.StartTime, state.EndDate, state.EndTime, state.GetUserTimeZone(), calendarService);
                    var searchedEvents = new List<EventModel>();
                    bool searchTodayMeeting = SearchesTodayMeeting(state);
                    foreach (var item in results)
                    {
                        if (!searchTodayMeeting || item.StartTime >= DateTime.UtcNow)
                        {
                            searchedEvents.Add(item);
                        }
                    }

                    if (searchedEvents.Count == 0)
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowNoMeetingMessage));
                        state.Clear();
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        if (options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain)
                        {
                            var responseParams = new StringDictionary()
                            {
                                { "Count", searchedEvents.Count.ToString() },
                                { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today }
                            };
                            if (searchedEvents.Count == 1)
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowOneMeetingSummaryAgainMessage, responseParams));
                            }
                            else
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowMeetingSummaryAgainMessage, responseParams));
                            }
                        }
                        else
                        {
                            var responseParams = new StringDictionary()
                            {
                                { "Count", searchedEvents.Count.ToString() },
                                { "EventName1", searchedEvents[0].Title },
                                { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today },
                                { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(searchedEvents[0].StartTime, state.GetUserTimeZone()), searchedEvents[0].IsAllDay == true) },
                                { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(searchedEvents[0].Attendees) }
                            };

                            if (searchedEvents.Count == 1)
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowOneMeetingSummaryMessage, responseParams));
                            }
                            else
                            {
                                responseParams.Add("EventName2", searchedEvents[searchedEvents.Count - 1].Title);
                                responseParams.Add("EventTime2", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(searchedEvents[searchedEvents.Count - 1].StartTime, state.GetUserTimeZone()), searchedEvents[searchedEvents.Count - 1].IsAllDay == true));
                                responseParams.Add("Participants2", DisplayHelper.ToDisplayParticipantsStringSummary(searchedEvents[searchedEvents.Count - 1].Attendees));

                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowMultipleMeetingSummaryMessage, responseParams));
                            }
                        }
                    }

                    await ShowMeetingList(sc, GetCurrentPageMeetings(searchedEvents, state), !searchTodayMeeting);
                    state.SummaryEvents = searchedEvents;
                    if (state.SummaryEvents.Count == 1)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions());
                    }
                }
                else
                {
                    var currentPageMeetings = GetCurrentPageMeetings(state.SummaryEvents, state);
                    if (options != null && options.Reason == ShowMeetingReason.ShowFilteredMeetings)
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowMultipleFilteredMeetings, new StringDictionary() { { "Count", state.SummaryEvents.Count.ToString() } }));
                    }
                    else
                    {
                        var responseParams = new StringDictionary()
                        {
                            { "Count", state.SummaryEvents.Count.ToString() },
                            { "EventName1", currentPageMeetings[0].Title },
                            { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today },
                            { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(currentPageMeetings[0].StartTime, state.GetUserTimeZone()), currentPageMeetings[0].IsAllDay == true) },
                            { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(currentPageMeetings[0].Attendees) }
                        };
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowMeetingSummaryNotFirstPageMessage, responseParams));
                    }

                    await ShowMeetingList(sc, GetCurrentPageMeetings(state.SummaryEvents, state), !SearchesTodayMeeting(state));
                }

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.ReadOutMorePrompt) });
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

        public async Task<DialogTurnResult> CallReadEventDialog(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, sc.Options);
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

                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, sc.Options);
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
                    state.ReadOutEvents = new List<EventModel>() { state.SummaryEvents[0] };
                }
                else if (state.SummaryEvents.Count == 1)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if (state.SummaryEvents.Count > 1 && (state.ReadOutEvents == null || state.ReadOutEvents.Count <= 0))
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
                    if (filteredMeetingList.Count <= 0 && luisResult.Entities.ContactName != null)
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
                        state.ReadOutEvents = filteredMeetingList;
                        return await sc.BeginDialogAsync(Actions.Read, sc.Options);
                    }
                    else if (filteredMeetingList.Count > 1)
                    {
                        state.SummaryEvents = filteredMeetingList;
                        return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowFilteredMeetings, sc.Options));
                    }
                }

                if (state.ReadOutEvents != null && state.ReadOutEvents.Count > 0)
                {
                    return await sc.BeginDialogAsync(Actions.Read, sc.Options);
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

        public async Task<DialogTurnResult> ReadEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                var eventItem = state.ReadOutEvents.FirstOrDefault();

                if (eventItem != null && topIntent != Luis.CalendarLU.Intent.ChangeCalendarEntry && topIntent != Luis.CalendarLU.Intent.DeleteCalendarEntry)
                {
                    var tokens = new StringDictionary()
                    {
                        { "Date", eventItem.StartTime.ToString(CommonStrings.DisplayDateFormat_CurrentYear) },
                        { "Time", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()), eventItem.IsAllDay == true) },
                        { "Participants", DisplayHelper.ToDisplayParticipantsStringSummary(eventItem.Attendees) },
                        { "Subject", eventItem.Title }
                    };

                    var card = new Card(eventItem.OnlineMeetingUrl == null ? "CalendarCardNoJoinButton" : "CalendarCard", eventItem.ToAdaptiveCardData(state.GetUserTimeZone(), showContent: true));
                    var replyMessage = ResponseManager.GetCardResponse(SummaryResponses.ReadOutMessage, card, tokens);
                    await sc.Context.SendActivityAsync(replyMessage);

                    if (eventItem.IsOrganizer)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForOrgnizerAction, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }) });
                    }
                    else if (eventItem.IsAccepted)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForAction, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }) });
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForChangeStatus, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }) });
                    }
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

        public async Task<DialogTurnResult> AfterReadOutEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;

                if (state.ReadOutEvents.Count > 0)
                {
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
                        state.ReadOutEvents = null;
                        state.SummaryEvents = null;
                        return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options));
                    }

                    var readoutEvent = state.ReadOutEvents[0];
                    state.ReadOutEvents = null;
                    state.SummaryEvents = null;
                    if (readoutEvent.IsOrganizer)
                    {
                        if (topIntent == CalendarLU.Intent.ChangeCalendarEntry)
                        {
                            state.Events.Add(readoutEvent);
                            state.IsActionFromSummary = true;
                            return await sc.BeginDialogAsync(nameof(UpdateEventDialog), sc.Options);
                        }

                        if (topIntent == CalendarLU.Intent.DeleteCalendarEntry)
                        {
                            state.Events.Add(readoutEvent);
                            state.IsActionFromSummary = true;
                            return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), sc.Options);
                        }

                        state.Clear();
                        return await sc.CancelAllDialogsAsync();
                    }
                    else if (readoutEvent.IsAccepted)
                    {
                        if (topIntent == CalendarLU.Intent.DeleteCalendarEntry)
                        {
                            state.Events.Add(readoutEvent);
                            state.IsActionFromSummary = true;
                            return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), sc.Options);
                        }

                        state.Clear();
                        return await sc.CancelAllDialogsAsync();
                    }
                    else
                    {
                        if (topIntent == CalendarLU.Intent.DeleteCalendarEntry || topIntent == CalendarLU.Intent.AcceptEventEntry)
                        {
                            state.Events.Add(readoutEvent);
                            state.IsActionFromSummary = true;
                            return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), sc.Options);
                        }

                        state.Clear();
                        return await sc.CancelAllDialogsAsync();
                    }
                }

                state.Clear();
                return await sc.CancelAllDialogsAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> ShowNextEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var askParameter = new AskParameterModel(state.AskParameterContent);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

                var eventList = await calendarService.GetUpcomingEvents();
                var nextEventList = new List<EventModel>();
                foreach (var item in eventList)
                {
                    if (item.IsCancelled != true && (nextEventList.Count == 0 || nextEventList[0].StartTime == item.StartTime))
                    {
                        nextEventList.Add(item);
                    }
                }

                if (nextEventList.Count == 0)
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowNoMeetingMessage));
                }
                else
                {
                    if (nextEventList.Count == 1)
                    {
                        // if user asked for specific details
                        if (askParameter.NeedDetail)
                        {
                            var tokens = new StringDictionary()
                            {
                                { "EventName", nextEventList[0].Title },
                                { "EventStartTime", TimeConverter.ConvertUtcToUserTime(nextEventList[0].StartTime, state.GetUserTimeZone()).ToString("h:mm tt") },
                                { "EventEndTime", TimeConverter.ConvertUtcToUserTime(nextEventList[0].EndTime, state.GetUserTimeZone()).ToString("h:mm tt") },
                                { "EventDuration", nextEventList[0].ToDurationString() },
                                { "EventLocation", nextEventList[0].Location },
                            };

                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.BeforeShowEventDetails, tokens));

                            if (askParameter.NeedTime)
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ReadTime, tokens));
                            }

                            if (askParameter.NeedDuration)
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ReadDuration, tokens));
                            }

                            if (askParameter.NeedLocation)
                            {
                                // for some event there might be no localtion.
                                if (string.IsNullOrEmpty(tokens["EventLocation"]))
                                {
                                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ReadNoLocation));
                                }
                                else
                                {
                                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ReadLocation, tokens));
                                }
                            }
                        }

                        var speakParams = new StringDictionary()
                        {
                            { "EventName", nextEventList[0].Title },
                            { "PeopleCount", nextEventList[0].Attendees.Count.ToString() },
                        };

                        speakParams.Add("EventTime", SpeakHelper.ToSpeechMeetingDateTime(TimeConverter.ConvertUtcToUserTime(nextEventList[0].StartTime, state.GetUserTimeZone()), nextEventList[0].IsAllDay == true));

                        if (string.IsNullOrEmpty(nextEventList[0].Location))
                        {
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowNextMeetingNoLocationMessage, speakParams));
                        }
                        else
                        {
                            speakParams.Add("Location", nextEventList[0].Location);
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowNextMeetingMessage, speakParams));
                        }
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowMultipleNextMeetingMessage));
                    }

                    await ShowMeetingList(sc, nextEventList, true);
                }

                state.Clear();
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

        public async Task<DialogTurnResult> AskForShowOverview(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(SummaryResponses.AskForShowOverview, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }),
                    RetryPrompt = ResponseManager.GetResponse(SummaryResponses.AskForShowOverview, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } })
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterAskForShowOverview(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var result = (bool)sc.Result;
                if (result)
                {
                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options));
                }
                else
                {
                    var state = await Accessor.GetAsync(sc.Context);
                    state.Clear();
                    return await sc.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private List<EventModel> GetCurrentPageMeetings(List<EventModel> allMeetings, CalendarSkillState state)
        {
            return allMeetings.GetRange(state.ShowEventIndex * state.PageSize, Math.Min(state.PageSize, allMeetings.Count - (state.ShowEventIndex * state.PageSize)));
        }

        private bool SearchesTodayMeeting(CalendarSkillState state)
        {
            var userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
            var searchDate = userNow;

            if (state.StartDate.Any())
            {
                searchDate = state.StartDate.Last();
            }

            return !state.StartTime.Any() &&
                !state.EndDate.Any() &&
                !state.EndTime.Any() &&
                EventModel.IsSameDate(searchDate, userNow);
        }
    }
}