// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using static CalendarSkill.Models.DialogOptions.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class ShowEventsDialog : CalendarSkillDialogBase
    {
        public ShowEventsDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            UpdateEventDialog updateEventDialog,
            ChangeEventStatusDialog changeEventStatusDialog,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(ShowEventsDialog), settings, services, conversationState, localeTemplateEngineManager, serviceManager, telemetryClient, appCredentials)
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

            var connectToMeeting = new WaterfallStep[]
            {
                ConnectToMeeting,
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
            AddDialog(new WaterfallDialog(Actions.ConnectToMeeting, connectToMeeting) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Read, readEvent) { TelemetryClient = telemetryClient });
            AddDialog(new WaterfallDialog(Actions.Reshow, reshow) { TelemetryClient = telemetryClient });
            AddDialog(updateEventDialog ?? throw new ArgumentNullException(nameof(updateEventDialog)));
            AddDialog(changeEventStatusDialog ?? throw new ArgumentNullException(nameof(changeEventStatusDialog)));

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
                if (state.MeetingInfo.OrderReference != null && state.MeetingInfo.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                {
                    options.Reason = ShowMeetingReason.ShowNextMeeting;
                }
                else
                {
                    // set default search date
                    if (!state.MeetingInfo.StartDate.Any() && IsOnlySearchByTime(state))
                    {
                        state.MeetingInfo.StartDate.Add(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone()));
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
            if (!string.IsNullOrEmpty(state.MeetingInfo.Title))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(state.MeetingInfo.Location))
            {
                return false;
            }

            if (state.MeetingInfo.ContactInfor.ContactsNameList.Any())
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
                    foreach (var item in state.ShowMeetingInfo.ShowingMeetings)
                    {
                        if (item.StartTime >= DateTime.UtcNow)
                        {
                            searchedEvents.Add(item);
                        }
                    }

                    state.ShowMeetingInfo.ShowingMeetings = searchedEvents;
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
                if (!state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ShowNoMeetingMessage);
                    await sc.Context.SendActivityAsync(activity);
                    state.Clear();
                    return await sc.EndDialogAsync(true);
                }

                if (options != null && options.Reason == ShowMeetingReason.ShowNextMeeting)
                {
                    return await sc.BeginDialogAsync(Actions.ShowNextEvent, options);
                }
                else if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
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
                if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                {
                    var askParameter = new AskParameterModel(state.ShowMeetingInfo.AskParameterContent);
                    if (askParameter.NeedDetail)
                    {
                        var tokens = new
                        {
                            EventName = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                            EventStartDate = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()).ToString(CalendarCommonStrings.DisplayDateLong),
                            EventStartTime = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime),
                            EventEndTime = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].EndTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime),
                            EventDuration = state.ShowMeetingInfo.ShowingMeetings[0].ToSpeechDurationString(),
                            EventLocation = state.ShowMeetingInfo.ShowingMeetings[0].Location
                        };

                        var activityBeforeShowEventDetails = TemplateEngine.GenerateActivityForLocale(SummaryResponses.BeforeShowEventDetails, tokens);
                        await sc.Context.SendActivityAsync(activityBeforeShowEventDetails);
                        if (askParameter.NeedTime)
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ReadTime, tokens);
                            await sc.Context.SendActivityAsync(activity);
                        }

                        if (askParameter.NeedDuration)
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ReadDuration, tokens);
                            await sc.Context.SendActivityAsync(activity);
                        }

                        if (askParameter.NeedLocation)
                        {
                            // for some event there might be no localtion.
                            if (string.IsNullOrEmpty(tokens.EventLocation))
                            {
                                var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ReadNoLocation, tokens);
                                await sc.Context.SendActivityAsync(activity);
                            }
                            else
                            {
                                var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ReadLocation, tokens);
                                await sc.Context.SendActivityAsync(activity);
                            }
                        }

                        if (askParameter.NeedDate)
                        {
                            var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ReadStartDate, tokens);
                            await sc.Context.SendActivityAsync(activity);
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
                if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                {
                    var speakParams = new
                    {
                        EventName = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        PeopleCount = state.ShowMeetingInfo.ShowingMeetings[0].Attendees.Count.ToString(),
                        EventTime = SpeakHelper.ToSpeechMeetingDateTime(
                            TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()),
                            state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Location = state.ShowMeetingInfo.ShowingMeetings[0].Location ?? string.Empty
                    };

                    if (string.IsNullOrEmpty(state.ShowMeetingInfo.ShowingMeetings[0].Location))
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ShowNextMeetingNoLocationMessage, speakParams);
                        await sc.Context.SendActivityAsync(activity);
                    }
                    else
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ShowNextMeetingMessage, speakParams);
                        await sc.Context.SendActivityAsync(activity);
                    }
                }
                else
                {
                    var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ShowMultipleNextMeetingMessage);
                    await sc.Context.SendActivityAsync(activity);
                }

                state.ShowMeetingInfo.ShowingCardTitle = CalendarCommonStrings.UpcommingMeeting;
                var reply = await GetGeneralMeetingListResponseAsync(sc, state, true);

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
                if (options.Reason == ShowMeetingReason.ShowOverviewAfterPageTurning)
                {
                    // show first meeting detail in response
                    var responseParams = new
                    {
                        Condition = GetSearchConditionString(state),
                        Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                        EventName1 = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower,
                        EventTime1 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Participants1 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[0].Attendees, 1)
                    };
                    string responseTemplateId = SummaryResponses.ShowMeetingSummaryNotFirstPageMessage;

                    await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc, responseTemplateId, responseParams));
                }
                else
                {
                    // if there are multiple meeting searched, show first and last meeting details in responses
                    var responseParams = new
                    {
                        Condition = GetSearchConditionString(state),
                        Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                        EventName1 = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower,
                        EventTime1 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Participants1 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[0].Attendees, 1),
                        EventName2 = state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].Title,
                        EventTime2 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].IsAllDay == true),
                        Participants2 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].Attendees, 1)
                    };
                    string responseTemplateId = string.Empty;
                    if (state.ShowMeetingInfo.Condition == CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Time)
                    {
                        responseTemplateId = SummaryResponses.ShowMultipleMeetingSummaryMessage;
                    }
                    else
                    {
                        responseTemplateId = SummaryResponses.ShowMeetingSummaryShortMessage;
                    }

                    await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc, responseTemplateId, responseParams));
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

        private async Task<DialogTurnResult> ShowEventsOverviewAgain(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                // when show overview again, won't show meeting details in response
                var responseParams = new
                {
                    Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                    Condition = GetSearchConditionString(state)
                };
                var responseTemplateId = SummaryResponses.ShowMeetingSummaryShortMessage;

                // await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc.Context, state, responseTemplateId, responseParams));
                await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc, responseTemplateId, responseParams));

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
                    sc, state, false,
                    SummaryResponses.ShowMultipleFilteredMeetings,
                    new { Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString() }));

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
                if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                {
                    // if only one meeting is showing, the prompt text is already included in show events step, prompt an empty message here
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions());
                }

                var prompt = TemplateEngine.GenerateActivityForLocale(SummaryResponses.ReadOutMorePrompt) as Activity;
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

                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var topIntent = luisResult?.TopIntent().intent;
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
                    state.ShowMeetingInfo.FocusedEvents.Add(state.ShowMeetingInfo.ShowingMeetings.First());
                    return await sc.BeginDialogAsync(Actions.Read);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    // answer no
                    state.Clear();
                    return await sc.EndDialogAsync();
                }

                if ((generalTopIntent == General.Intent.ShowNext || topIntent == CalendarLuis.Intent.ShowNextCalendar) && state.ShowMeetingInfo.ShowingMeetings != null)
                {
                    if ((state.ShowMeetingInfo.ShowEventIndex + 1) * state.PageSize < state.ShowMeetingInfo.ShowingMeetings.Count)
                    {
                        state.ShowMeetingInfo.ShowEventIndex++;
                    }
                    else
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.CalendarNoMoreEvent);
                        await sc.Context.SendActivityAsync(activity);
                    }

                    var options = sc.Options as ShowMeetingsDialogOptions;
                    options.Reason = ShowMeetingReason.ShowOverviewAfterPageTurning;
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, options);
                }
                else if ((generalTopIntent == General.Intent.ShowPrevious || topIntent == CalendarLuis.Intent.ShowPreviousCalendar) && state.ShowMeetingInfo.ShowingMeetings != null)
                {
                    if (state.ShowMeetingInfo.ShowEventIndex > 0)
                    {
                        state.ShowMeetingInfo.ShowEventIndex--;
                    }
                    else
                    {
                        var activity = TemplateEngine.GenerateActivityForLocale(SummaryResponses.CalendarNoPreviousEvent);
                        await sc.Context.SendActivityAsync(activity);
                    }

                    var options = sc.Options as ShowMeetingsDialogOptions;
                    options.Reason = ShowMeetingReason.ShowOverviewAfterPageTurning;
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, options);
                }
                else
                {
                    if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                    {
                        state.ShowMeetingInfo.FocusedEvents.Add(state.ShowMeetingInfo.ShowingMeetings[0]);
                    }
                    else
                    {
                        var filteredMeetingList = GetFilteredEvents(state, luisResult, userInput, sc.Context.Activity.Locale ?? English, out var showingCardTitle);

                        if (filteredMeetingList.Count == 1)
                        {
                            state.ShowMeetingInfo.FocusedEvents = filteredMeetingList;
                        }
                        else if (filteredMeetingList.Count > 1)
                        {
                            state.ShowMeetingInfo.Clear();
                            state.ShowMeetingInfo.ShowingCardTitle = showingCardTitle;
                            state.ShowMeetingInfo.ShowingMeetings = filteredMeetingList;
                            return await sc.ReplaceDialogAsync(Actions.ShowFilteredEvents, sc.Options);
                        }
                    }

                    var intentSwithingResult = await GetIntentSwitchingResult(sc, topIntent.Value, state);
                    if (intentSwithingResult != null)
                    {
                        return intentSwithingResult;
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.Read);
                    }
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

        private async Task<DialogTurnResult> GetIntentSwitchingResult(WaterfallStepContext sc, CalendarLuis.Intent topIntent, CalendarSkillState state)
        {
            var newFlowOptions = new CalendarSkillDialogOptions() { SubFlowMode = false };
            if (topIntent == CalendarLuis.Intent.DeleteCalendarEntry || topIntent == CalendarLuis.Intent.AcceptEventEntry)
            {
                return await sc.BeginDialogAsync(Actions.ChangeEventStatus);
            }
            else if (topIntent == CalendarLuis.Intent.ChangeCalendarEntry)
            {
                return await sc.BeginDialogAsync(Actions.UpdateEvent);
            }
            else if (topIntent == CalendarLuis.Intent.CheckAvailability)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(CheckPersonAvailableDialog), newFlowOptions);
            }
            else if (topIntent == CalendarLuis.Intent.ConnectToMeeting)
            {
                return await sc.BeginDialogAsync(Actions.ConnectToMeeting);
            }
            else if (topIntent == CalendarLuis.Intent.CreateCalendarEntry)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(CreateEventDialog), newFlowOptions);
            }
            else if (topIntent == CalendarLuis.Intent.FindCalendarDetail
                || topIntent == CalendarLuis.Intent.FindCalendarEntry
                || topIntent == CalendarLuis.Intent.FindCalendarWhen
                || topIntent == CalendarLuis.Intent.FindCalendarWhere
                || topIntent == CalendarLuis.Intent.FindCalendarWho
                || topIntent == CalendarLuis.Intent.FindDuration
                || topIntent == CalendarLuis.Intent.FindMeetingRoom)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(ShowEventsDialog), new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, newFlowOptions));
            }
            else if (topIntent == CalendarLuis.Intent.TimeRemaining)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(TimeRemainingDialog), newFlowOptions);
            }

            return null;
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

                if (!state.ShowMeetingInfo.FocusedEvents.Any())
                {
                    state.ShowMeetingInfo.FocusedEvents.Add(state.ShowMeetingInfo.ShowingMeetings.FirstOrDefault());
                    isShowingMeetingDetail = false;
                }

                var eventItem = state.ShowMeetingInfo.FocusedEvents.FirstOrDefault();

                if (isShowingMeetingDetail)
                {
                    var tokens = new
                    {
                        Date = eventItem.StartTime.ToString(CommonStrings.DisplayDateFormat_CurrentYear),
                        Time = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()), eventItem.IsAllDay == true),
                        Participants = DisplayHelper.ToDisplayParticipantsStringSummary(eventItem.Attendees, 1),
                        Subject = eventItem.Title
                    };

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, eventItem, SummaryResponses.ReadOutMessage, tokens);
                    await sc.Context.SendActivityAsync(replyMessage);
                }
                else
                {
                    var responseParams = new
                    {
                        Condition = GetSearchConditionString(state),
                        Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                        EventName1 = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower,
                        EventTime1 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Participants1 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[0].Attendees, 1)
                    };
                    string responseTemplateId = null;

                    if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                    {
                        if (state.ShowMeetingInfo.Condition == CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Time && !(options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain))
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

                var eventItem = state.ShowMeetingInfo.FocusedEvents.FirstOrDefault();

                if (eventItem.IsOrganizer)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = TemplateEngine.GenerateActivityForLocale(SummaryResponses.AskForOrgnizerAction, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
                    });
                }
                else if (eventItem.IsAccepted)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = TemplateEngine.GenerateActivityForLocale(SummaryResponses.AskForAction, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
                    });
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = TemplateEngine.GenerateActivityForLocale(SummaryResponses.AskForChangeStatus, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
                    });
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
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var topIntent = luisResult?.TopIntent().intent;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    state.ShowMeetingInfo.Clear();
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options));
                }
                else
                {
                    var intentSwithingResult = await GetIntentSwitchingResult(sc, topIntent.Value, state);
                    if (intentSwithingResult != null)
                    {
                        return intentSwithingResult;
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
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
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

        private async Task<DialogTurnResult> ConnectToMeeting(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var options = new CalendarSkillDialogOptions() { SubFlowMode = true };
                return await sc.BeginDialogAsync(nameof(JoinEventDialog), options);
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
                state.ShowMeetingInfo.Clear();
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateEngine.GenerateActivityForLocale(SummaryResponses.AskForShowOverview, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity,
                    RetryPrompt = TemplateEngine.GenerateActivityForLocale(SummaryResponses.AskForShowOverview, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
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

            if (state.MeetingInfo.StartDate.Any())
            {
                searchDate = state.MeetingInfo.StartDate.Last();
            }

            return !state.MeetingInfo.StartTime.Any() &&
                !state.MeetingInfo.EndDate.Any() &&
                !state.MeetingInfo.EndTime.Any() &&
                EventModel.IsSameDate(searchDate, userNow);
        }
    }
}