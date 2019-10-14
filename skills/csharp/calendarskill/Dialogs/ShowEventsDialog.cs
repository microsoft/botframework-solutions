// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Adapters;
using CalendarSkill.Middlewares;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Google.Apis.Calendar.v3.Data;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Builder.Solutions.Middleware;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using static CalendarSkill.Models.DialogOptions.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class ShowEventsDialog : CalendarSkillDialogBase
    {
        public ShowEventsDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            UpdateEventDialog updateEventDialog,
            ChangeEventStatusDialog changeEventStatusDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ShowEventsDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
        {
            TelemetryClient = telemetryClient;

            var showMeetings = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                SetSearchCondition,
            };

            var searchEvents = new WaterfallStep[]
            {
                SearchEventsWithEntities,
                FilterTodayEvent,
                AddConflictFlag,
                ShowAskParameterDetails,
                ShowEventsList
            };

            var showNextMeeting = new WaterfallStep[]
            {
                ShowNextMeeting,
            };

            var showEventsOverview = new WaterfallStep[]
            {
                ShowEventsOverview,
                PromptForNextAction,
                HandleNextAction
            };

            var showEventsOverviewAgain = new WaterfallStep[]
            {
                ShowEventsOverviewAgain,
                PromptForNextAction,
                HandleNextAction
            };

            var showFilteredEvents = new WaterfallStep[]
            {
                ShowFilteredEvents,
                PromptForNextAction,
                HandleNextAction
            };

            var readEvent = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                ReadEvent,
                PromptForNextActionAfterRead,
                HandleNextActionAfterRead,
            };

            var retryUnknown = new WaterfallStep[]
            {
                SendFallback,
                RetryInput,
                HandleNextAction,
            };

            var updateEvent = new WaterfallStep[]
            {
                UpdateEvent,
                ReShow
            };

            var changeEventStatus = new WaterfallStep[]
            {
                ChangeEventStatus,
                ReShow
            };

            var reshow = new WaterfallStep[]
            {
                AskForShowOverview,
                AfterAskForShowOverview
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowEvents, showMeetings) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.SearchEvents, searchEvents) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowNextEvent, showNextMeeting) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowEventsOverview, showEventsOverview) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowEventsOverviewAgain, showEventsOverviewAgain) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowFilteredEvents, showFilteredEvents) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChangeEventStatus, changeEventStatus) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateEvent, updateEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Read, readEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Reshow, reshow) { TelemetryClient = telemetryClient });
            AddDialog(updateEventDialog ?? throw new ArgumentNullException(nameof(updateEventDialog)));
            AddDialog(changeEventStatusDialog ?? throw new ArgumentNullException(nameof(changeEventStatusDialog)));
            AddDialog(new WaterfallDialog(Actions.RetryUnknown, retryUnknown) { TelemetryClient = telemetryClient });
            AddDialog(new EventPrompt(Actions.FallbackEventPrompt, SkillEvents.FallbackHandledEventName, ResponseValidatorAsync));

            // Set starting dialog for component
            InitialDialogId = Actions.ShowEvents;
        }

        private async Task<DialogTurnResult> SetSearchCondition(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // if show next meeting
                if (state.MeetingInfor.OrderReference != null && state.MeetingInfor.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                {
                    options.Reason = ShowMeetingReason.ShowNextMeeting;
                }
                else
                {
                    // set default search date
                    if (!state.MeetingInfor.StartDate.Any() && IsOnlySearchByTime(state))
                    {
                        state.MeetingInfor.StartDate.Add(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone()));
                    }
                }

                return await sc.BeginDialogAsync(Actions.SearchEvents, options);
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

        private bool IsOnlySearchByTime(CalendarSkillState state)
        {
            if (!string.IsNullOrEmpty(state.MeetingInfor.Title))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(state.MeetingInfor.Location))
            {
                return false;
            }

            if (state.MeetingInfor.ContactInfor.ContactsNameList.Any())
            {
                return false;
            }

            return true;
        }

        private async Task<DialogTurnResult> FilterTodayEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // if user query meetings in today, only show the meeting is upcoming in today.
                var state = await Accessor.GetAsync(sc.Context);
                var isSearchedTodayMeeting = IsSearchedTodayMeeting(state);
                if (isSearchedTodayMeeting)
                {
                    var searchedEvents = new List<EventModel>();
                    foreach (var item in state.ShowMeetingInfor.ShowingMeetings)
                    {
                        if (item.StartTime >= DateTime.UtcNow)
                        {
                            searchedEvents.Add(item);
                        }
                    }

                    state.ShowMeetingInfor.ShowingMeetings = searchedEvents;
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

        private async Task<DialogTurnResult> ShowEventsList(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // no meeting
                if (!state.ShowMeetingInfor.ShowingMeetings.Any())
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowNoMeetingMessage));
                    state.Clear();

                    sc.Context.SetTurnName("NoMeeting");

                    return await sc.EndDialogAsync(true);
                }

                if (options != null && options.Reason == ShowMeetingReason.ShowNextMeeting)
                {
                    return await sc.BeginDialogAsync(Actions.ShowNextEvent, options);
                }
                else if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                {
                    return await sc.BeginDialogAsync(Actions.Read, options);
                }
                else if (options != null && (options.Reason == ShowMeetingReason.FirstShowOverview || options.Reason == ShowMeetingReason.ShowOverviewAfterPageTurning))
                {
                    return await sc.BeginDialogAsync(Actions.ShowEventsOverview, options);
                }
                else if (options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain)
                {
                    return await sc.BeginDialogAsync(Actions.ShowEventsOverviewAgain, options);
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

        private async Task<DialogTurnResult> ShowAskParameterDetails(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // this step will answer the question like: "when is my next meeting", next meeting is only one meeting will have answer
                if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                {
                    var askParameter = new AskParameterModel(state.ShowMeetingInfor.AskParameterContent);
                    if (askParameter.NeedDetail)
                    {
                        var tokens = new StringDictionary()
                        {
                            { "EventName", state.ShowMeetingInfor.ShowingMeetings[0].Title },
                            { "EventStartDate", TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.ShowingMeetings[0].StartTime, state.GetUserTimeZone()).ToString(CalendarCommonStrings.DisplayDateLong) },
                            { "EventStartTime", TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.ShowingMeetings[0].StartTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime) },
                            { "EventEndTime", TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.ShowingMeetings[0].EndTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime) },
                            { "EventDuration", state.ShowMeetingInfor.ShowingMeetings[0].ToSpeechDurationString() },
                            { "EventLocation", state.ShowMeetingInfor.ShowingMeetings[0].Location },
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

                        if (askParameter.NeedDate)
                        {
                            await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ReadStartDate, tokens));
                        }
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

        private async Task<DialogTurnResult> ShowNextMeeting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // if only one next meeting, show the meeting detail card, otherwise show a meeting list card
                if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                {
                    var speakParams = new StringDictionary()
                    {
                        { "EventName", state.ShowMeetingInfor.ShowingMeetings[0].Title },
                        { "PeopleCount", state.ShowMeetingInfor.ShowingMeetings[0].Attendees.Count.ToString() },
                    };

                    speakParams.Add("EventTime", SpeakHelper.ToSpeechMeetingDateTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfor.ShowingMeetings[0].IsAllDay == true));

                    if (string.IsNullOrEmpty(state.ShowMeetingInfor.ShowingMeetings[0].Location))
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowNextMeetingNoLocationMessage, speakParams));
                    }
                    else
                    {
                        speakParams.Add("Location", state.ShowMeetingInfor.ShowingMeetings[0].Location);
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowNextMeetingMessage, speakParams));
                    }
                }
                else
                {
                    await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.ShowMultipleNextMeetingMessage));
                }

                state.ShowMeetingInfor.ShowingCardTitle = CalendarCommonStrings.UpcommingMeeting;
                var reply = await GetGeneralMeetingListResponseAsync(sc.Context, state, true);

                await sc.Context.SendActivityAsync(reply);

                state.Clear();
                return await sc.EndDialogAsync();
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

        private async Task<DialogTurnResult> ShowEventsOverview(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // show first meeting detail in response
                var responseParams = new StringDictionary()
                {
                    { "Condition", GetSearchConditionString(state) },
                    { "Count", state.ShowMeetingInfor.ShowingMeetings.Count.ToString() },
                    { "EventName1", state.ShowMeetingInfor.ShowingMeetings[0].Title },
                    { "DateTime", state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower },
                    { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfor.ShowingMeetings[0].IsAllDay == true) },
                    { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfor.ShowingMeetings[0].Attendees, 1) }
                };
                string responseTemplateId;

                if (options.Reason == ShowMeetingReason.ShowOverviewAfterPageTurning)
                {
                    responseTemplateId = SummaryResponses.ShowMeetingSummaryNotFirstPageMessage;
                }
                else
                {
                    // if there are multiple meeting searched, show first and last meeting details in responses
                    responseParams.Add("EventName2", state.ShowMeetingInfor.ShowingMeetings[state.ShowMeetingInfor.ShowingMeetings.Count - 1].Title);
                    responseParams.Add("EventTime2", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.ShowingMeetings[state.ShowMeetingInfor.ShowingMeetings.Count - 1].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfor.ShowingMeetings[state.ShowMeetingInfor.ShowingMeetings.Count - 1].IsAllDay == true));
                    responseParams.Add("Participants2", DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfor.ShowingMeetings[state.ShowMeetingInfor.ShowingMeetings.Count - 1].Attendees, 1));

                    if (state.ShowMeetingInfor.Condition == CalendarSkillState.ShowMeetingInformation.SearchMeetingCondition.Time)
                    {
                        responseTemplateId = SummaryResponses.ShowMultipleMeetingSummaryMessage;
                    }
                    else
                    {
                        responseTemplateId = SummaryResponses.ShowMeetingSummaryShortMessage;
                    }
                }

                await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc.Context, state, responseTemplateId, responseParams));

                sc.Context.SetTurnName("ShowEventsOverview");

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

        private async Task<DialogTurnResult> ShowEventsOverviewAgain(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // when show overview again, won't show meeting details in response
                var responseParams = new StringDictionary()
                {
                    { "Count", state.ShowMeetingInfor.ShowingMeetings.Count.ToString() },
                    { "Condition", GetSearchConditionString(state) },
                };
                var responseTemplateId = SummaryResponses.ShowMeetingSummaryShortMessage;
                await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc.Context, state, responseTemplateId, responseParams));

                sc.Context.SetTurnName("ShowEventsOverview");

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

        private async Task<DialogTurnResult> ShowFilteredEvents(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // show filtered meeting with general event list
                await sc.Context.SendActivityAsync(await GetGeneralMeetingListResponseAsync(
                    sc.Context, state, false,
                    SummaryResponses.ShowMultipleFilteredMeetings,
                    new StringDictionary() { { "Count", state.ShowMeetingInfor.ShowingMeetings.Count.ToString() } }));

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

        private async Task<DialogTurnResult> PromptForNextAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                {
                    // if only one meeting is showing, the prompt text is already included in show events step, prompt an empty message here
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions());
                }

                var prompt = ResponseManager.GetResponse("ReadOutMorePrompt");
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt });
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

        private async Task<DialogTurnResult> HandleNextAction(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

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

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    // answer yes
                    state.ShowMeetingInfor.FocusedEvents.Add(state.ShowMeetingInfor.ShowingMeetings.First());
                    return await sc.BeginDialogAsync(Actions.Read);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    // answer no
                    state.Clear();
                    return await sc.EndDialogAsync();
                }

                if ((generalTopIntent == General.Intent.ShowNext || topIntent == CalendarLuis.Intent.ShowNextCalendar) && state.ShowMeetingInfor.ShowingMeetings != null)
                {
                    if ((state.ShowMeetingInfor.ShowEventIndex + 1) * state.PageSize < state.ShowMeetingInfor.ShowingMeetings.Count)
                    {
                        state.ShowMeetingInfor.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoMoreEvent));
                    }

                    var options = sc.Options as ShowMeetingsDialogOptions;
                    options.Reason = ShowMeetingReason.ShowOverviewAfterPageTurning;
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, options);
                }
                else if ((generalTopIntent == General.Intent.ShowPrevious || topIntent == CalendarLuis.Intent.ShowPreviousCalendar) && state.ShowMeetingInfor.ShowingMeetings != null)
                {
                    if (state.ShowMeetingInfor.ShowEventIndex > 0)
                    {
                        state.ShowMeetingInfor.ShowEventIndex--;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(ResponseManager.GetResponse(SummaryResponses.CalendarNoPreviousEvent));
                    }

                    var options = sc.Options as ShowMeetingsDialogOptions;
                    options.Reason = ShowMeetingReason.ShowOverviewAfterPageTurning;
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, options);
                }
                else
                {
                    if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                    {
                        state.ShowMeetingInfor.FocusedEvents.Add(state.ShowMeetingInfor.ShowingMeetings[0]);
                    }
                    else
                    {
                        var filteredMeetingList = GetFilteredEvents(state, userInput, sc.Context.Activity.Locale ?? English, out var showingCardTitle);

                        if (filteredMeetingList.Count == 1)
                        {
                            state.ShowMeetingInfor.FocusedEvents = filteredMeetingList;
                        }
                        else if (filteredMeetingList.Count > 1)
                        {
                            state.ShowMeetingInfor.Clear();
                            state.ShowMeetingInfor.ShowingCardTitle = showingCardTitle;
                            state.ShowMeetingInfor.ShowingMeetings = filteredMeetingList;
                            return await sc.ReplaceDialogAsync(Actions.ShowFilteredEvents, sc.Options);
                        }
                    }

                    if (state.ShowMeetingInfor.FocusedEvents.Count == 1)
                    {
                        if (topIntent == CalendarLuis.Intent.DeleteCalendarEntry || topIntent == CalendarLuis.Intent.AcceptEventEntry)
                        {
                            return await sc.BeginDialogAsync(Actions.ChangeEventStatus);
                        }
                        else if (topIntent == CalendarLuis.Intent.ChangeCalendarEntry)
                        {
                            return await sc.BeginDialogAsync(Actions.UpdateEvent);
                        }
                        else
                        {
                            return await sc.BeginDialogAsync(Actions.Read);
                        }
                    }

                    return await sc.ReplaceDialogAsync(Actions.RetryUnknown, sc.Options);
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

        private async Task<DialogTurnResult> ReadEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // show a meeting detail card for the focused meeting
                var state = await Accessor.GetAsync(sc.Context);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // if isShowingMeetingDetail is true, will show the response of showing meeting detail. Otherwise will use show one summary meeting response.
                var isShowingMeetingDetail = true;

                if (!state.ShowMeetingInfor.FocusedEvents.Any())
                {
                    state.ShowMeetingInfor.FocusedEvents.Add(state.ShowMeetingInfor.ShowingMeetings.FirstOrDefault());
                    isShowingMeetingDetail = false;
                }

                var eventItem = state.ShowMeetingInfor.FocusedEvents.FirstOrDefault();

                if (isShowingMeetingDetail)
                {
                    var tokens = new StringDictionary()
                    {
                        { "Date", eventItem.StartTime.ToString(CommonStrings.DisplayDateFormat_CurrentYear) },
                        { "Time", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()), eventItem.IsAllDay == true) },
                        { "Participants", DisplayHelper.ToDisplayParticipantsStringSummary(eventItem.Attendees, 1) },
                        { "Subject", eventItem.Title }
                    };

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, eventItem, SummaryResponses.ReadOutMessage, tokens);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    var responseParams = new StringDictionary()
                    {
                        { "Condition", GetSearchConditionString(state) },
                        { "Count", state.ShowMeetingInfor.ShowingMeetings.Count.ToString() },
                        { "EventName1", state.ShowMeetingInfor.ShowingMeetings[0].Title },
                        { "DateTime", state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower },
                        { "EventTime1", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfor.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfor.ShowingMeetings[0].IsAllDay == true) },
                        { "Participants1", DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfor.ShowingMeetings[0].Attendees, 1) }
                    };
                    string responseTemplateId = null;

                    if (state.ShowMeetingInfor.ShowingMeetings.Count == 1)
                    {
                        if (state.ShowMeetingInfor.Condition == CalendarSkillState.ShowMeetingInformation.SearchMeetingCondition.Time && !(options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain))
                        {
                            responseTemplateId = SummaryResponses.ShowOneMeetingSummaryMessage;
                        }
                        else
                        {
                            responseTemplateId = SummaryResponses.ShowOneMeetingSummaryShortMessage;
                        }
                    }

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, eventItem, responseTemplateId, responseParams);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                sc.Context.SetTurnName("ReadEvent");

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

        private async Task<DialogTurnResult> PromptForNextActionAfterRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var eventItem = state.ShowMeetingInfor.FocusedEvents.FirstOrDefault();

                if (eventItem.IsOrganizer)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForOrgnizerAction, new StringDictionary() { { "DateTime", state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower } }) });
                }
                else if (eventItem.IsAccepted)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForAction, new StringDictionary() { { "DateTime", state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower } }) });
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(SummaryResponses.AskForChangeStatus, new StringDictionary() { { "DateTime", state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower } }) });
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

        private async Task<DialogTurnResult> HandleNextActionAfterRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    state.ShowMeetingInfor.Clear();
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options));
                }
                else
                {
                    if (topIntent == CalendarLuis.Intent.DeleteCalendarEntry || topIntent == CalendarLuis.Intent.AcceptEventEntry)
                    {
                        return await sc.BeginDialogAsync(Actions.ChangeEventStatus);
                    }
                    else if (topIntent == CalendarLuis.Intent.ChangeCalendarEntry)
                    {
                        return await sc.BeginDialogAsync(Actions.UpdateEvent);
                    }
                }

                state.Clear();
                return await sc.CancelAllDialogsAsync();
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

        private async Task<DialogTurnResult> ReShow(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.Reshow);
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

        private async Task<DialogTurnResult> UpdateEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var options = new CalendarSkillDialogOptions() { SubFlowMode = true };
                return await sc.BeginDialogAsync(nameof(UpdateEventDialog), options);
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

        private async Task<DialogTurnResult> ChangeEventStatus(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                if (topIntent == CalendarLuis.Intent.AcceptEventEntry)
                {
                    var options = new ChangeEventStatusDialogOptions(new CalendarSkillDialogOptions() { SubFlowMode = true }, EventStatus.Accepted);
                    return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), options);
                }
                else
                {
                    var options = new ChangeEventStatusDialogOptions(new CalendarSkillDialogOptions() { SubFlowMode = true }, EventStatus.Cancelled);
                    return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), options);
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

        private async Task<DialogTurnResult> AskForShowOverview(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                state.ShowMeetingInfor.Clear();
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = ResponseManager.GetResponse(SummaryResponses.AskForShowOverview, new StringDictionary() { { "DateTime", state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower } }),
                    RetryPrompt = ResponseManager.GetResponse(SummaryResponses.AskForShowOverview, new StringDictionary() { { "DateTime", state.MeetingInfor.StartDateString ?? CalendarCommonStrings.TodayLower } })
                }, cancellationToken);
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

        private async Task<DialogTurnResult> AfterAskForShowOverview(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var result = (bool)sc.Result;
                if (result)
                {
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options));
                }
                else
                {
                    var state = await Accessor.GetAsync(sc.Context);
                    state.Clear();
                    return await sc.EndDialogAsync();
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

        private bool IsSearchedTodayMeeting(CalendarSkillState state)
        {
            var userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
            var searchDate = userNow;

            if (state.MeetingInfor.StartDate.Any())
            {
                searchDate = state.MeetingInfor.StartDate.Last();
            }

            return !state.MeetingInfor.StartTime.Any() &&
                !state.MeetingInfor.EndDate.Any() &&
                !state.MeetingInfor.EndTime.Any() &&
                EventModel.IsSameDate(searchDate, userNow);
        }

        private Task<bool> ResponseValidatorAsync(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event && activity.Name == SkillEvents.FallbackHandledEventName)
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        private async Task<DialogTurnResult> SendFallback(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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

        private async Task<DialogTurnResult> RetryInput(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = ResponseManager.GetResponse(CalendarSharedResponses.RetryInput) });
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
    }
}