﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.ChangeEventStatus;
using CalendarSkill.Dialogs.Shared;
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
using Microsoft.Bot.Solutions.Data;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace CalendarSkill.Dialogs.Summary
{
    public class SummaryDialog : CalendarSkillDialog
    {
        public SummaryDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(SummaryDialog), services, accessor, serviceManager, telemetryClient)
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
                IfClearContextStep,
                ShowEventsSummary,
                PromptToRead,
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
            AddDialog(new UpdateEventDialog(services, accessor, serviceManager, telemetryClient));
            AddDialog(new ChangeEventStatusDialog(services, accessor, serviceManager, telemetryClient));

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

        public async Task<DialogTurnResult> IfClearContextStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // clear context before show emails, and extract it from luis result again.
                var state = await Accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;

                var generalLuisResult = state.GeneralLuisResult;
                var topGeneralIntent = generalLuisResult?.TopIntent().intent;

                if (topGeneralIntent == General.Intent.Next && state.SummaryEvents != null)
                {
                    if ((state.ShowEventIndex + 1) * state.PageSize < state.SummaryEvents.Count)
                    {
                        state.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.CalendarNoMoreEvent));
                        return await sc.CancelAllDialogsAsync();
                    }
                }
                else if (topGeneralIntent == General.Intent.Previous && state.SummaryEvents != null)
                {
                    if (state.ShowEventIndex > 0)
                    {
                        state.ShowEventIndex--;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.CalendarNoPreviousEvent));
                        return await sc.CancelAllDialogsAsync();
                    }
                }
                else
                {
                    state.SummaryEvents = null;
                }

                return await sc.NextAsync();
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
                    bool searchTodayMeeting = state.StartDate.Any() &&
                        !state.StartTime.Any() &&
                        !state.EndDate.Any() &&
                        !state.EndTime.Any() &&
                        EventModel.IsSameDate(searchDate, TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone()));
                    foreach (var item in results)
                    {
                        if (!searchTodayMeeting || item.StartTime >= DateTime.UtcNow)
                        {
                            searchedEvents.Add(item);
                        }
                    }

                    if (searchedEvents.Count == 0)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowNoMeetingMessage));
                        state.Clear();
                        return await sc.EndDialogAsync(true);
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
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowOneMeetingSummaryMessage, ResponseBuilder, responseParams));
                        }
                        else
                        {
                            responseParams.Add("EventName2", searchedEvents[searchedEvents.Count - 1].Title);
                            responseParams.Add("EventTime2", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(searchedEvents[searchedEvents.Count - 1].StartTime, state.GetUserTimeZone()), searchedEvents[searchedEvents.Count - 1].IsAllDay == true));
                            responseParams.Add("Participants2", DisplayHelper.ToDisplayParticipantsStringSummary(searchedEvents[searchedEvents.Count - 1].Attendees));

                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowMultipleMeetingSummaryMessage, ResponseBuilder, responseParams));
                        }
                    }

                    await ShowMeetingList(sc, searchedEvents.GetRange(0, Math.Min(state.PageSize, searchedEvents.Count)), false);
                    state.SummaryEvents = searchedEvents;
                }
                else
                {
                    await ShowMeetingList(sc, state.SummaryEvents.GetRange(state.ShowEventIndex * state.PageSize, Math.Min(state.PageSize, state.SummaryEvents.Count - (state.ShowEventIndex * state.PageSize))), false);
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

        public async Task<DialogTurnResult> PromptToRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.SummaryEvents.Count == 1)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions());
                }

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.ReadOutMorePrompt) });
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
                return await sc.BeginDialogAsync(Actions.Read);
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

                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (topIntent == null)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }

                if (generalTopIntent == General.Intent.Next || generalTopIntent == General.Intent.Previous)
                {
                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary);
                }

                var eventItem = state.ReadOutEvents.FirstOrDefault();

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    state.Clear();
                    return await sc.CancelAllDialogsAsync();
                }
                else if ((promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true) || state.SummaryEvents.Count == 1)
                {
                    eventItem = state.SummaryEvents[0];
                }
                else if (topIntent == Luis.Calendar.Intent.ReadAloud && eventItem == null)
                {
                    if (state.SummaryEvents.Count == 1)
                    {
                        eventItem = state.SummaryEvents[0];
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.ReadOutPrompt), });
                    }
                }

                if (eventItem != null && topIntent != Luis.Calendar.Intent.ChangeCalendarEntry && topIntent != Luis.Calendar.Intent.DeleteCalendarEntry)
                {
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(
                        SummaryResponses.ReadOutMessage,
                        eventItem.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json",
                        eventItem.ToAdaptiveCardData(state.GetUserTimeZone(), showContent: true),
                        null,
                        new StringDictionary()
                        {
                            {
                                "Date", eventItem.StartTime.ToString(CommonStrings.DisplayDateFormat_CurrentYear)
                            },
                            {
                                "Time", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()), eventItem.IsAllDay == true)
                            },
                            {
                                "Participants", DisplayHelper.ToDisplayParticipantsStringSummary(eventItem.Attendees)
                            },
                            {
                                "Subject", eventItem.Title
                            }
                        });
                    await sc.Context.SendActivityAsync(replyMessage);

                    if (eventItem.IsOrganizer)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.AskForOrgnizerAction, ResponseBuilder, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }) });
                    }
                    else if (eventItem.IsAccepted)
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.AskForAction, ResponseBuilder, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }) });
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.AskForChangeStatus, ResponseBuilder, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }) });
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
                        return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary);
                    }

                    var readoutEvent = state.ReadOutEvents[0];
                    if (readoutEvent.IsOrganizer)
                    {
                        if (topIntent == Calendar.Intent.ChangeCalendarEntry)
                        {
                            state.Events.Add(readoutEvent);
                            return await sc.BeginDialogAsync(nameof(UpdateEventDialog));
                        }

                        if (topIntent == Calendar.Intent.DeleteCalendarEntry)
                        {
                            state.Events.Add(readoutEvent);
                            return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog));
                        }

                        state.Clear();
                        return await sc.CancelAllDialogsAsync();
                    }
                    else if (readoutEvent.IsAccepted)
                    {
                        if (topIntent == Calendar.Intent.DeleteCalendarEntry)
                        {
                            state.Events.Add(readoutEvent);
                            return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog));
                        }

                        state.Clear();
                        return await sc.CancelAllDialogsAsync();
                    }
                    else
                    {
                        if (topIntent == Calendar.Intent.DeleteCalendarEntry || topIntent == Calendar.Intent.AcceptEventEntry)
                        {
                            state.Events.Add(readoutEvent);
                            return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog));
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
                AskParameterModel askParameter = new AskParameterModel(state.AskParameterContent);
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
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowNoMeetingMessage));
                }
                else
                {
                    if (nextEventList.Count == 1)
                    {
                        // if user asked for specific details
                        if (askParameter.NeedDetail)
                        {
                            var responseParams = new StringDictionary()
                            {
                                { "EventName", nextEventList[0].Title },
                                { "EventStartTime", TimeConverter.ConvertUtcToUserTime(nextEventList[0].StartTime, state.GetUserTimeZone()).ToString("h:mm tt") },
                                { "EventEndTime", TimeConverter.ConvertUtcToUserTime(nextEventList[0].EndTime, state.GetUserTimeZone()).ToString("h:mm tt") },
                                { "EventDuration", nextEventList[0].ToDurationString() },
                                { "EventLocation", nextEventList[0].Location },
                            };

                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.BeforeShowEventDetails, ResponseBuilder, responseParams));

                            if (askParameter.NeedTime)
                            {
                                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ReadTime, ResponseBuilder, responseParams));
                            }

                            if (askParameter.NeedDuration)
                            {
                                await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ReadDuration, ResponseBuilder, responseParams));
                            }

                            if (askParameter.NeedLocation)
                            {
                                // for some event there might be no localtion.
                                if (string.IsNullOrEmpty(responseParams["EventLocation"]))
                                {
                                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ReadNoLocation));
                                }
                                else
                                {
                                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ReadLocation, ResponseBuilder, responseParams));
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
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowNextMeetingNoLocationMessage, ResponseBuilder, speakParams));
                        }
                        else
                        {
                            speakParams.Add("Location", nextEventList[0].Location);
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowNextMeetingMessage, ResponseBuilder, speakParams));
                        }
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowMultipleNextMeetingMessage));
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
                    Prompt = sc.Context.Activity.CreateReply(SummaryResponses.AskForShowOverview, ResponseBuilder, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } }),
                    RetryPrompt = sc.Context.Activity.CreateReply(SummaryResponses.AskForShowOverview, ResponseBuilder, new StringDictionary() { { "DateTime", state.StartDateString ?? CalendarCommonStrings.Today } })
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
                    return await sc.ReplaceDialogAsync(Actions.ShowEventsSummary);
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
    }
}