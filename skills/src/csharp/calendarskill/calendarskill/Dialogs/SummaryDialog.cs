using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogModel;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Rules;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Steps;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using static CalendarSkill.Models.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class SummaryDialog : CalendarSkillDialogBase
    {
        public SummaryDialog(
           BotSettings settings,
           BotServices services,
           ResponseManager responseManager,
           ConversationState conversationState,
           UpdateEventDialog updateEventDialog,
           ChangeEventStatusDialog changeEventStatusDialog,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient)
           : base(nameof(SummaryDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var skillOptions = new CalendarSkillDialogOptions
            {
                SubFlowMode = true
            };

            var rootDialog = new AdaptiveDialog("ShowMeetingRootDialog")
            {
                Recognizer = CreateRecognizer(),
                Rules = new List<IRule>()
                {
                    new IntentRule("ChangeCalendarEntry")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(nameof(UpdateEventDialog), options: skillOptions)
                        }
                    },
                    new IntentRule("DeleteCalendarEntry")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(nameof(ChangeEventStatusDialog), options: skillOptions)
                        }
                    },
                    new IntentRule("AcceptCalendarEntry")
                    {
                        Steps = new List<IDialog>()
                        {
                            new BeginDialog(nameof(ChangeEventStatusDialog), options: skillOptions)
                        }
                    }
                },
                Steps = new List<IDialog>()
                {
                    new BeginDialog(Actions.GetEventsInit)
                }
            };

            var initStep = new WaterfallStep[]
            {
                SaveShowMeetingsDialogState,
                Init,
            };

            var showNext = new WaterfallStep[]
            {
                SaveShowMeetingsDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                ShowNextEvent,
            };

            var showSummary = new WaterfallStep[]
            {
                SaveShowMeetingsDialogState,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEventsList,
                CallReadEventDialog,
                AskForShowOverview,
                AfterAskForShowOverview
            };

            var readEvent = new WaterfallStep[]
            {
                SaveShowMeetingsDialogState,
                ReadEvent,
                AfterReadOutEvent
            };

            var initDialog = new CalendarWaterfallDialog(Actions.GetEventsInit, initStep, CalendarStateAccessor) { TelemetryClient = telemetryClient };
            var showNextDialog = new CalendarWaterfallDialog(Actions.ShowNextEvent, showNext, CalendarStateAccessor) { TelemetryClient = telemetryClient };
            var showEventsSummaryDialog = new CalendarWaterfallDialog(Actions.ShowEventsSummary, showSummary, CalendarStateAccessor) { TelemetryClient = telemetryClient };
            var readDialog = new CalendarWaterfallDialog(Actions.Read, readEvent, CalendarStateAccessor) { TelemetryClient = telemetryClient };

            // Define the conversation flow using a waterfall model.
            AddDialog(initDialog);
            AddDialog(showNextDialog);
            AddDialog(showEventsSummaryDialog);
            AddDialog(readDialog);
            AddDialog(updateEventDialog ?? throw new ArgumentNullException(nameof(updateEventDialog)));
            AddDialog(changeEventStatusDialog ?? throw new ArgumentNullException(nameof(changeEventStatusDialog)));

            // Set starting dialog for component
            //InitialDialogId = Actions.GetEventsInit;
            AddDialog(rootDialog);
            rootDialog.AddDialog(new List<IDialog>() {
                initDialog,
                showNextDialog,
                showEventsSummaryDialog,
                readDialog
            });
            InitialDialogId = "ShowMeetingRootDialog";
        }

        private static IRecognizer CreateRecognizer()
        {
            return new LuisRecognizer(new LuisApplication()
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/807cd523-34cb-4911-b149-cdcb58f661cc?verbose=true&timezoneOffset=-360&subscription-key=80d731206676475bb03d30e3bc2ee07e&q=",//Configuration["LuisAPIHostName"],
                EndpointKey = "80d731206676475bb03d30e3bc2ee07e", //Configuration["LuisAPIKey"],
                ApplicationId = "807cd523-34cb-4911-b149-cdcb58f661cc",// Configuration["LuisAppId"]
            });
        }

        public async Task<DialogTurnResult> Init(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var dialogState = (ShowMeetingsDialogState)sc.State.Dialog[CalendarStateKey];
                if (dialogState.OrderReference != null && dialogState.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var tokenResponse = sc.Result as TokenResponse;

                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (ShowMeetingsDialogState)sc.State.Dialog[CalendarStateKey];

                var options = sc.Options as ShowMeetingsDialogOptions;
                if (dialogState.SummaryEvents == null)
                {
                    var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);

                    var searchDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userState.GetUserTimeZone());

                    if (dialogState.StartDate.Any())
                    {
                        searchDate = dialogState.StartDate.Last();
                    }

                    var results = await GetEventsByTime(new List<DateTime>() { searchDate }, dialogState.StartTime, dialogState.EndDate, dialogState.EndTime, userState.GetUserTimeZone(), calendarService);
                    var searchedEvents = new List<EventModel>();
                    var searchTodayMeeting = SearchesTodayMeeting(dialogState, userState);
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
                        await ClearAllState(sc.Context);
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        if (options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain)
                        {
                            var responseParams = new StringDictionary()
                            {
                                { "Count", searchedEvents.Count.ToString() },
                                { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower }
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
                                { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower },
                                { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(searchedEvents[0].StartTime, userState.GetUserTimeZone()), searchedEvents[0].IsAllDay == true) },
                                { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(searchedEvents[0].Attendees, 1) }
                            };

                            if (searchedEvents.Count == 1)
                            {
                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowOneMeetingSummaryMessage, responseParams));
                            }
                            else
                            {
                                responseParams.Add("EventName2", searchedEvents[searchedEvents.Count - 1].Title);
                                responseParams.Add("EventTime2", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(searchedEvents[searchedEvents.Count - 1].StartTime, userState.GetUserTimeZone()), searchedEvents[searchedEvents.Count - 1].IsAllDay == true));
                                responseParams.Add("Participants2", DisplayHelper.ToDisplayParticipantsStringSummary(searchedEvents[searchedEvents.Count - 1].Attendees, 1));

                                await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowMultipleMeetingSummaryMessage, responseParams));
                            }
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

                    dialogState.TotalConflictCount = totalConflictCount;

                    await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(
                        sc,
                        GetCurrentPageMeetings(searchedEvents, dialogState, userState, out var firstIndex, out var lastIndex),
                        firstIndex,
                        lastIndex,
                        searchedEvents.Count,
                        totalConflictCount,
                        null,
                        null));
                    dialogState.SummaryEvents = searchedEvents;
                    if (dialogState.SummaryEvents.Count == 1)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions());
                    }
                }
                else
                {
                    var currentPageMeetings = GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState);
                    if (options != null && (
                        options.Reason == ShowMeetingReason.ShowFilteredByTitleMeetings ||
                        options.Reason == ShowMeetingReason.ShowFilteredByTimeMeetings ||
                        options.Reason == ShowMeetingReason.ShowFilteredByParticipantNameMeetings))
                    {
                        string meetingListTitle = null;

                        if (options.Reason == ShowMeetingReason.ShowFilteredByTitleMeetings)
                        {
                            meetingListTitle = string.Format(CalendarCommonStrings.MeetingsAbout, dialogState.FilterMeetingKeyWord);
                        }
                        else if (options.Reason == ShowMeetingReason.ShowFilteredByTimeMeetings)
                        {
                            meetingListTitle = string.Format(CalendarCommonStrings.MeetingsAt, dialogState.FilterMeetingKeyWord);
                        }
                        else if (options.Reason == ShowMeetingReason.ShowFilteredByParticipantNameMeetings)
                        {
                            meetingListTitle = string.Format(CalendarCommonStrings.MeetingsWith, dialogState.FilterMeetingKeyWord);
                        }

                        var reply = await GetGeneralMeetingListResponseAsync(
                            sc,
                            meetingListTitle,
                            dialogState.SummaryEvents,
                            SummaryResponses.ShowMultipleFilteredMeetings,
                            new StringDictionary() { { "Count", dialogState.SummaryEvents.Count.ToString() } });
                        await sc.Context.SendActivityAsync(reply);
                    }
                    else
                    {
                        var responseParams = new StringDictionary()
                        {
                            { "Count", dialogState.SummaryEvents.Count.ToString() },
                            { "EventName1", currentPageMeetings[0].Title },
                            { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower },
                            { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(currentPageMeetings[0].StartTime, userState.GetUserTimeZone()), currentPageMeetings[0].IsAllDay == true) },
                            { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(currentPageMeetings[0].Attendees, 1) }
                        };
                        var reply = await GetOverviewMeetingListResponseAsync(
                            sc,
                            GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState, out var firstIndex, out var lastIndex),
                            firstIndex,
                            lastIndex,
                            dialogState.SummaryEvents.Count,
                            dialogState.TotalConflictCount,
                            SummaryResponses.ShowMeetingSummaryNotFirstPageMessage,
                            responseParams);

                        await sc.Context.SendActivityAsync(reply);
                    }
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (ShowMeetingsDialogState)sc.State.Dialog[CalendarStateKey];
                skillOptions.DialogState = dialogState;

                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = userState.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (topIntent == null)
                {
                    await ClearAllState(sc.Context);
                    return await sc.CancelAllDialogsAsync();
                }

                if ((generalTopIntent == General.Intent.ShowNext || topIntent == CalendarLuis.Intent.ShowNextCalendar) && dialogState.SummaryEvents != null)
                {
                    if ((dialogState.ShowEventIndex + 1) * userState.PageSize < dialogState.SummaryEvents.Count)
                    {
                        dialogState.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoMoreEvent));
                    }

                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, sc.Options);
                }
                else if ((generalTopIntent == General.Intent.ShowPrevious || topIntent == CalendarLuis.Intent.ShowPreviousCalendar) && dialogState.SummaryEvents != null)
                {
                    if (dialogState.ShowEventIndex > 0)
                    {
                        dialogState.ShowEventIndex--;
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
                    await ClearAllState(sc.Context);
                    return await sc.EndDialogAsync(true);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    dialogState.ReadOutEvents = new List<EventModel>() { dialogState.SummaryEvents[0] };
                }
                else if (dialogState.SummaryEvents.Count == 1)
                {
                    await ClearAllState(sc.Context);
                    return await sc.EndDialogAsync(true);
                }

                if (dialogState.SummaryEvents.Count > 1 && (dialogState.ReadOutEvents == null || dialogState.ReadOutEvents.Count <= 0))
                {
                    var filteredMeetingList = new List<EventModel>();
                    ShowMeetingReason showMeetingReason = ShowMeetingReason.FirstShowOverview;
                    string filterKeyWord = null;

                    // filter meetings with number
                    if (generalLuisResult.Entities.ordinal != null)
                    {
                        var value = generalLuisResult.Entities.ordinal[0];
                        var num = int.Parse(value.ToString());
                        var currentList = GetCurrentPageMeetings(dialogState.SummaryEvents, dialogState, userState);
                        if (num > 0 && num <= currentList.Count)
                        {
                            filteredMeetingList.Add(currentList[num - 1]);
                        }
                    }

                    if (filteredMeetingList.Count <= 0 && generalLuisResult.Entities.number != null && (generalLuisResult.Entities.ordinal == null || generalLuisResult.Entities.ordinal.Length == 0))
                    {
                        var value = generalLuisResult.Entities.number[0];
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
                        dialogState.ReadOutEvents = filteredMeetingList;
                        return await sc.BeginDialogAsync(Actions.Read, skillOptions);
                    }
                    else if (filteredMeetingList.Count > 1)
                    {
                        dialogState.FilterMeetingKeyWord = filterKeyWord;
                        dialogState.SummaryEvents = filteredMeetingList;
                        return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, new ShowMeetingsDialogOptions(showMeetingReason, skillOptions));
                    }
                }

                if (dialogState.ReadOutEvents != null && dialogState.ReadOutEvents.Count > 0)
                {
                    return await sc.BeginDialogAsync(Actions.Read, skillOptions);
                }
                else
                {
                    await ClearAllState(sc.Context);
                    return await sc.EndDialogAsync(true);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (ShowMeetingsDialogState)sc.State.Dialog[CalendarStateKey];

                var luisResult = userState.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                // save the readout event for next action dialog
                dialogState.Events = dialogState.ReadOutEvents;

                var eventItem = dialogState.ReadOutEvents.FirstOrDefault();

                if (eventItem != null && topIntent != Luis.CalendarLuis.Intent.ChangeCalendarEntry && topIntent != Luis.CalendarLuis.Intent.DeleteCalendarEntry)
                {
                    var tokens = new StringDictionary()
                    {
                        { "Date", eventItem.StartTime.ToString(CommonStrings.DisplayDateFormat_CurrentYear) },
                        { "Time", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, userState.GetUserTimeZone()), eventItem.IsAllDay == true) },
                        { "Participants", DisplayHelper.ToDisplayParticipantsStringSummary(eventItem.Attendees, 1) },
                        { "Subject", eventItem.Title }
                    };

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, eventItem, SummaryResponses.ReadOutMessage, tokens);
                    await sc.Context.SendActivityAsync(replyMessage);

                    if (eventItem.IsOrganizer)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForOrgnizerAction, new StringDictionary() { { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower } }) });
                    }
                    else if (eventItem.IsAccepted)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForAction, new StringDictionary() { { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower } }) });
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForChangeStatus, new StringDictionary() { { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower } }) });
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (ShowMeetingsDialogState)sc.State.Dialog[CalendarStateKey];

                dialogState.Events = dialogState.ReadOutEvents;

                var luisResult = userState.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;

                if (dialogState.ReadOutEvents.Count > 0)
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                    if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                    {
                        await ClearAllState(sc.Context);
                        return await sc.CancelAllDialogsAsync();
                    }
                    else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                    {
                        dialogState.ReadOutEvents = null;
                        dialogState.SummaryEvents = null;
                        return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, skillOptions));
                    }
                    else
                    {
                        await ClearAllState(sc.Context);
                        return await sc.CancelAllDialogsAsync();
                    }
                }
                else
                {
                    await ClearAllState(sc.Context);
                    return await sc.CancelAllDialogsAsync();
                }
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var userState = await CalendarStateAccessor.GetAsync(sc.Context);
                var dialogState = (ShowMeetingsDialogState)sc.State.Dialog[CalendarStateKey];

                var askParameter = new AskParameterModel(dialogState.AskParameterContent);
                if (string.IsNullOrEmpty(userState.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarService = ServiceManager.InitCalendarService(userState.APIToken, userState.EventSource);

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
                                { "EventStartTime", TimeConverter.ConvertUtcToUserTime(nextEventList[0].StartTime, userState.GetUserTimeZone()).ToString("h:mm tt") },
                                { "EventEndTime", TimeConverter.ConvertUtcToUserTime(nextEventList[0].EndTime, userState.GetUserTimeZone()).ToString("h:mm tt") },
                                { "EventDuration", nextEventList[0].ToSpeechDurationString() },
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

                        speakParams.Add("EventTime", SpeakHelper.ToSpeechMeetingDateTime(TimeConverter.ConvertUtcToUserTime(nextEventList[0].StartTime, userState.GetUserTimeZone()), nextEventList[0].IsAllDay == true));

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

                    var reply = await GetGeneralMeetingListResponseAsync(sc, CalendarCommonStrings.UpcommingMeeting, nextEventList, null, null);

                    await sc.Context.SendActivityAsync(reply);
                }

                await ClearAllState(sc.Context);
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
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var dialogState = (ShowMeetingsDialogState)sc.State.Dialog[CalendarStateKey];

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(SummaryResponses.AskForShowOverview, new StringDictionary() { { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower } }),
                    RetryPrompt = ResponseManager.GetResponse(SummaryResponses.AskForShowOverview, new StringDictionary() { { "DateTime", dialogState.StartDateString ?? CalendarCommonStrings.TodayLower } })
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
                    await ClearAllState(sc.Context);
                    return await sc.EndDialogAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private bool SearchesTodayMeeting(ShowMeetingsDialogState dialogState, CalendarSkillState userState)
        {
            var userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userState.GetUserTimeZone());
            var searchDate = userNow;

            if (dialogState.StartDate.Any())
            {
                searchDate = dialogState.StartDate.Last();
            }

            return !dialogState.StartTime.Any() &&
                !dialogState.EndDate.Any() &&
                !dialogState.EndTime.Any() &&
                EventModel.IsSameDate(searchDate, userNow);
        }

        private async Task<DialogTurnResult> SaveShowMeetingsDialogState(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                var dialogState = skillOptions?.DialogState != null ? skillOptions?.DialogState : new ShowMeetingsDialogState();

                if (skillOptions != null && skillOptions.DialogState != null)
                {
                    if (skillOptions.DialogState is ShowMeetingsDialogState)
                    {
                        dialogState = (ShowMeetingsDialogState)skillOptions.DialogState;
                    }
                    else
                    {
                        dialogState = skillOptions.DialogState != null ? new ShowMeetingsDialogState(skillOptions.DialogState) : dialogState;
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

                var newState = await DigestShowMeetingsLuisResult(sc, userState.LuisResult, userState.GeneralLuisResult, dialogState as ShowMeetingsDialogState, true);
                sc.State.Dialog.Add(CalendarStateKey, newState);

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<ShowMeetingsDialogState> DigestShowMeetingsLuisResult(DialogContext dc, CalendarLuis luisResult, General generalLuisResult, ShowMeetingsDialogState state, bool isBeginDialog)
        {
            try
            {
                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;

                var userState = await CalendarStateAccessor.GetAsync(dc.Context);

                if (!isBeginDialog)
                {
                    return state;
                }

                switch (intent)
                {
                    case CalendarLuis.Intent.FindCalendarEntry:
                    case CalendarLuis.Intent.FindCalendarDetail:
                    case CalendarLuis.Intent.FindCalendarWhen:
                    case CalendarLuis.Intent.FindCalendarWhere:
                    case CalendarLuis.Intent.FindCalendarWho:
                    case CalendarLuis.Intent.FindDuration:
                        {
                            if (entity.OrderReference != null)
                            {
                                state.OrderReference = GetOrderReferenceFromEntity(entity);
                            }

                            if (entity.FromDate != null)
                            {
                                var dateString = GetDateTimeStringFromInstanceData(luisResult.Text, entity._instance.FromDate[0]);
                                var date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), true);
                                if (date != null)
                                {
                                    state.StartDate = date;
                                    state.StartDateString = dateString;
                                }

                                date = GetDateFromDateTimeString(dateString, dc.Context.Activity.Locale, userState.GetUserTimeZone(), false);
                                if (date != null)
                                {
                                    state.EndDate = date;
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

                            state.AskParameterContent = luisResult.Text;

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