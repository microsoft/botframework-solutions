using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Adapters;
using CalendarSkill.Models;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using static CalendarSkill.Models.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class SummaryDialog : CalendarSkillDialogBase
    {
        private ResourceMultiLanguageGenerator _lgMultiLangEngine;

        public SummaryDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UpdateEventDialog updateEventDialog,
            ChangeEventStatusDialog changeEventStatusDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(SummaryDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            _lgMultiLangEngine = new ResourceMultiLanguageGenerator("SummaryDialog.lg");
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
                ShowEventsList,
                FilterMeeting,
                HandleActions,
                AskForShowOverview,
                AfterAskForShowOverview
            };

            var readEvent = new WaterfallStep[]
            {
                ReadEvent,
                AfterReadOutEvent,
            };

            var retryUnknown = new WaterfallStep[]
            {
                SendFallback,
                RetryInput,
                HandleActions,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.GetEventsInit, initStep) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowNextEvent, showNext) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowEventsSummary, showSummary) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Read, readEvent) { TelemetryClient = telemetryClient });
            AddDialog(updateEventDialog ?? throw new ArgumentNullException(nameof(updateEventDialog)));
            AddDialog(changeEventStatusDialog ?? throw new ArgumentNullException(nameof(changeEventStatusDialog)));
            AddDialog(new WaterfallDialog(Actions.RetryUnknown, retryUnknown) { TelemetryClient = telemetryClient });
            AddDialog(new EventPrompt(Actions.FallbackEventPrompt, SkillEvents.FallbackHandledEventName, ResponseValidatorAsync));

            // Set starting dialog for component
            InitialDialogId = Actions.GetEventsInit;
        }

        public async Task<DialogTurnResult> Init(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.OrderReference != null && state.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
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

        public async Task<DialogTurnResult> ShowEventsList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    var searchTodayMeeting = SearchesTodayMeeting(state);
                    foreach (var item in results)
                    {
                        if (!searchTodayMeeting || item.StartTime >= DateTime.UtcNow)
                        {
                            searchedEvents.Add(item);
                        }
                    }

                    if (searchedEvents.Count == 0)
                    {
                        await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ShowNoMeetingMessage]", null));
                        state.Clear();
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        if (options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain)
                        {
                            var responseParams = new
                            {
                                count = searchedEvents.Count,
                                dateTimeString = state.StartDateString ?? CalendarCommonStrings.TodayLower
                            };

                            await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ShowMeetingSummaryAgainMessage]", responseParams));
                        }
                        else
                        {
                            var responseParams = new
                            {
                                events = searchedEvents,
                                timezone = state.GetUserTimeZone().Id,
                                dateTimeString = state.StartDateString ?? CalendarCommonStrings.TodayLower
                            };

                            await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ShowMeetingSummaryMessage]", responseParams));
                        }
                    }

                    // add conflict flag
                    for (var i = 0; i < searchedEvents.Count - 1; i++)
                    {
                        for (var j = i + 1; j < searchedEvents.Count; j++)
                        {
                            if (searchedEvents[i].StartTime <= searchedEvents[j].StartTime &&
                                searchedEvents[i].EndTime > searchedEvents[j].StartTime)
                            {
                                searchedEvents[i].IsConflict = true;
                                searchedEvents[j].IsConflict = true;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    // count the conflict meetings
                    var totalConflictCount = 0;
                    foreach (var eventItem in searchedEvents)
                    {
                        if (eventItem.IsConflict)
                        {
                            totalConflictCount++;
                        }
                    }

                    state.TotalConflictCount = totalConflictCount;

                    await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(
                        sc,
                        _lgMultiLangEngine,
                        GetCurrentPageMeetings(searchedEvents, state, out var firstIndex, out var lastIndex),
                        firstIndex,
                        lastIndex,
                        searchedEvents.Count,
                        totalConflictCount,
                        null,
                        null));
                    state.SummaryEvents = searchedEvents;
                    if (state.SummaryEvents.Count == 1)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions());
                    }
                }
                else
                {
                    var currentPageMeetings = GetCurrentPageMeetings(state.SummaryEvents, state);
                    if (options != null && (
                        options.Reason == ShowMeetingReason.ShowFilteredByTitleMeetings ||
                        options.Reason == ShowMeetingReason.ShowFilteredByTimeMeetings ||
                        options.Reason == ShowMeetingReason.ShowFilteredByParticipantNameMeetings))
                    {
                        string meetingListTitle = null;

                        if (options.Reason == ShowMeetingReason.ShowFilteredByTitleMeetings)
                        {
                            meetingListTitle = string.Format(CalendarCommonStrings.MeetingsAbout, state.FilterMeetingKeyWord);
                        }
                        else if (options.Reason == ShowMeetingReason.ShowFilteredByTimeMeetings)
                        {
                            meetingListTitle = string.Format(CalendarCommonStrings.MeetingsAt, state.FilterMeetingKeyWord);
                        }
                        else if (options.Reason == ShowMeetingReason.ShowFilteredByParticipantNameMeetings)
                        {
                            meetingListTitle = string.Format(CalendarCommonStrings.MeetingsWith, state.FilterMeetingKeyWord);
                        }

                        var reply = await GetGeneralMeetingListResponseAsync(
                            sc,
                            _lgMultiLangEngine,
                            meetingListTitle,
                            state.SummaryEvents,
                            "ShowMultipleFilteredMeetings",
                            new { count = state.SummaryEvents.Count.ToString() });
                        await sc.Context.SendActivityAsync(reply);
                    }
                    else
                    {
                        var responseParams = new
                        {
                            events = currentPageMeetings,
                            timezone = state.GetUserTimeZone().Id
                        };

                        var reply = await GetOverviewMeetingListResponseAsync(
                            sc,
                            _lgMultiLangEngine,
                            GetCurrentPageMeetings(state.SummaryEvents, state, out var firstIndex, out var lastIndex),
                            firstIndex,
                            lastIndex,
                            state.SummaryEvents.Count,
                            state.TotalConflictCount,
                            "ShowMeetingSummaryNotFirstPageMessage",
                            responseParams);

                        await sc.Context.SendActivityAsync(reply);
                    }
                }

                var prompt = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ReadOutMorePrompt]", null);

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = (Activity)prompt });
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

        public async Task<DialogTurnResult> FilterMeeting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (topIntent == null)
                {
                    state.Clear();
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    state.Clear();
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    state.FocusEvents = new List<EventModel>() { state.SummaryEvents[0] };
                }

                if (state.SummaryEvents.Count > 1 && (state.FocusEvents == null || state.FocusEvents.Count <= 0))
                {
                    var filteredMeetingList = new List<EventModel>();
                    ShowMeetingReason showMeetingReason = ShowMeetingReason.FirstShowOverview;
                    string filterKeyWord = null;

                    // filter meetings with number
                    if (state.UserSelectIndex >= 0)
                    {
                        var currentList = GetCurrentPageMeetings(state.SummaryEvents, state);
                        if (state.UserSelectIndex < currentList.Count)
                        {
                            filteredMeetingList.Add(currentList[state.UserSelectIndex]);
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
                                    foreach (var meeting in GetCurrentPageMeetings(state.SummaryEvents, state))
                                    {
                                        if (meeting.StartTime.TimeOfDay == utcStartTime.TimeOfDay)
                                        {
                                            showMeetingReason = ShowMeetingReason.ShowFilteredByTimeMeetings;
                                            filterKeyWord = string.Format("{0:H:mm}", dateTime);
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

                        foreach (var meeting in GetCurrentPageMeetings(state.SummaryEvents, state))
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
                                showMeetingReason = ShowMeetingReason.ShowFilteredByParticipantNameMeetings;
                                filterKeyWord = string.Join(", ", contactNameList);
                                filteredMeetingList.Add(meeting);
                            }
                        }
                    }

                    if (filteredMeetingList.Count == 1)
                    {
                        state.FocusEvents = filteredMeetingList;
                        return await sc.NextAsync();
                    }
                    else if (filteredMeetingList.Count > 1)
                    {
                        state.FilterMeetingKeyWord = filterKeyWord;
                        state.SummaryEvents = filteredMeetingList;
                        return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, new ShowMeetingsDialogOptions(showMeetingReason, sc.Options));
                    }
                }

                return await sc.NextAsync(true);
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

                var eventItem = state.FocusEvents.FirstOrDefault();

                if (eventItem != null && topIntent != Luis.CalendarLuis.Intent.ChangeCalendarEntry && topIntent != Luis.CalendarLuis.Intent.DeleteCalendarEntry)
                {
                    var replyParams = new
                    {
                        startDateTime = eventItem.StartTime,
                        timezone = state.GetUserTimeZone().Id,
                        attendees = eventItem.Attendees,
                        subject = eventItem.Title
                    };
                    var replyMessage = await GetDetailMeetingResponseAsync(sc, _lgMultiLangEngine, eventItem, "ReadOutMessage", replyParams);

                    await sc.Context.SendActivityAsync(replyMessage);

                    var askForActionData = new
                    {
                        isOrganizer = eventItem.IsOrganizer,
                        isAccecpt = eventItem.IsAccepted,
                        startDateString = state.StartDateString
                    };

                    var askForActionParams = new
                    {
                        isOrganizer = eventItem.IsOrganizer,
                        isAccepted = eventItem.IsAccepted,
                        startDateString = state.StartDateString
                    };
                    var askForActionPrompt = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AskForAction]", null);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = (Activity)askForActionPrompt });
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

                if (state.FocusEvents.Count > 0)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                    if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                    {
                        state.FocusEvents = null;
                        state.SummaryEvents = null;
                        return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options));
                    }

                    var readoutEvent = state.FocusEvents[0];
                    state.FocusEvents = null;
                    state.SummaryEvents = null;
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

        public async Task<DialogTurnResult> HandleActions(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (topIntent == null)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if ((generalTopIntent == General.Intent.ShowNext || topIntent == CalendarLuis.Intent.ShowNextCalendar) && state.SummaryEvents != null)
                {
                    if ((state.ShowEventIndex + 1) * state.PageSize < state.SummaryEvents.Count)
                    {
                        state.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[CalendarNoMoreEvent]", null));
                    }

                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, sc.Options);
                }
                else if ((generalTopIntent == General.Intent.ShowPrevious || topIntent == CalendarLuis.Intent.ShowPreviousCalendar) && state.SummaryEvents != null)
                {
                    if (state.ShowEventIndex > 0)
                    {
                        state.ShowEventIndex--;
                    }
                    else
                    {
                        var prompt = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[CalendarNoPreviousEvent]", null);

                        await sc.Context.SendActivityAsync(prompt);
                    }

                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, sc.Options);
                }

                if (state.FocusEvents.Count <= 0)
                {
                    var currentList = GetCurrentPageMeetings(state.SummaryEvents, state);
                    if (state.UserSelectIndex >= 0 && state.UserSelectIndex < currentList.Count)
                    {
                        state.FocusEvents.Add(currentList[state.UserSelectIndex]);
                    }
                }

                if (state.FocusEvents != null && state.FocusEvents.Count > 0)
                {
                    var focusEvent = state.FocusEvents.First();
                    if (focusEvent != null)
                    {
                        if (focusEvent.IsOrganizer)
                        {
                            if (topIntent == CalendarLuis.Intent.ChangeCalendarEntry)
                            {
                                state.Events.Add(focusEvent);
                                state.IsActionFromSummary = true;
                                return await sc.BeginDialogAsync(nameof(UpdateEventDialog), sc.Options);
                            }

                            if (topIntent == CalendarLuis.Intent.DeleteCalendarEntry)
                            {
                                state.Events.Add(focusEvent);
                                state.IsActionFromSummary = true;
                                return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), sc.Options);
                            }
                        }
                        else if (focusEvent.IsAccepted)
                        {
                            if (topIntent == CalendarLuis.Intent.DeleteCalendarEntry)
                            {
                                state.Events.Add(focusEvent);
                                state.IsActionFromSummary = true;
                                return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), sc.Options);
                            }
                        }
                        else
                        {
                            if (topIntent == CalendarLuis.Intent.DeleteCalendarEntry || topIntent == CalendarLuis.Intent.AcceptEventEntry)
                            {
                                state.Events.Add(focusEvent);
                                state.IsActionFromSummary = true;
                                return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), sc.Options);
                            }
                        }

                        return await sc.BeginDialogAsync(Actions.Read, sc.Options);
                    }
                }

                return await sc.ReplaceDialogAsync(Actions.RetryUnknown, sc.Options);
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
                    await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ShowNoMeetingMessage]", null));
                }
                else
                {
                    if (nextEventList.Count == 1)
                    {
                        // if user asked for specific details
                        if (askParameter.NeedDetail)
                        {
                            var data = new
                            {
                                meeting = nextEventList[0],
                                timezone = state.GetUserTimeZone().Id
                            };

                            await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[BeforeShowEventDetails]", data));

                            if (askParameter.NeedTime)
                            {
                                await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ReadTime]", data));
                            }

                            if (askParameter.NeedDuration)
                            {
                                await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ReadDuration]", data));
                            }

                            if (askParameter.NeedLocation)
                            {
                                await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ReadLocation]", data));
                            }
                        }

                        var showNextMeetingData = new
                        {
                            meeting = nextEventList[0],
                            timezone = state.GetUserTimeZone().Id
                        };

                        await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ShowNextMeetingMessage]", showNextMeetingData));
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[ShowMultipleNextMeetingMessage]", null));
                    }

                    var reply = await GetGeneralMeetingListResponseAsync(sc, _lgMultiLangEngine, CalendarCommonStrings.UpcommingMeeting, nextEventList, null, null);

                    await sc.Context.SendActivityAsync(reply);
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
                state.ClearSummaryList();

                var data = new
                {
                    startDateString = state.StartDateString
                };

                var prompt = await LGHelper.GenerateMessageAsync(_lgMultiLangEngine, sc.Context, "[AskForShowOverview]", data);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = (Activity)prompt,
                    RetryPrompt = (Activity)prompt
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
            return GetCurrentPageMeetings(allMeetings, state, out var firstIndex, out var lastIndex);
        }

        private List<EventModel> GetCurrentPageMeetings(List<EventModel> allMeetings, CalendarSkillState state, out int firstIndex, out int lastIndex)
        {
            firstIndex = state.ShowEventIndex * state.PageSize;
            lastIndex = Math.Min(state.PageSize, allMeetings.Count - (state.ShowEventIndex * state.PageSize));
            return allMeetings.GetRange(firstIndex, lastIndex);
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

        protected Task<bool> ResponseValidatorAsync(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event && activity.Name == SkillEvents.FallbackHandledEventName)
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        protected async Task<DialogTurnResult> SendFallback(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // Send Fallback Event
                if (sc.Context.Adapter is CalendarSkillWebSocketBotAdapter remoteInvocationAdapter)
                {
                    await remoteInvocationAdapter.SendRemoteFallbackEventAsync(sc.Context, cancellationToken).ConfigureAwait(false);

                    // Wait for the FallbackHandle event
                    return await sc.PromptAsync(Actions.FallbackEventPrompt, new PromptOptions()).ConfigureAwait(false);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> RetryInput(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(CalendarSharedResponses.RetryInput) });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}