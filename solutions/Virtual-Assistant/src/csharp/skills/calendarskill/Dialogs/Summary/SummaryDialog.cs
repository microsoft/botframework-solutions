﻿using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Summary.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill
{
    public class SummaryDialog : CalendarSkillDialog
    {
        public SummaryDialog(
            SkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(SummaryDialog), services, accessor, serviceManager)
        {
            var showSummary = new WaterfallStep[]
            {
                IfClearContextStep,
                GetAuthToken,
                AfterGetAuthToken,
                ShowEventsSummary,
                PromptToRead,
                CallReadEventDialog,
            };

            var readEvent = new WaterfallStep[]
            {
                ReadEvent,
                AfterReadOutEvent,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowEventsSummary, showSummary));
            AddDialog(new WaterfallDialog(Actions.Read, readEvent));

            // Set starting dialog for component
            InitialDialogId = Actions.ShowEventsSummary;
        }

        public async Task<DialogTurnResult> IfClearContextStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // clear context before show emails, and extract it from luis result again.
                var state = await _accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                    
                var topIntent = luisResult?.TopIntent().intent;

                if (topIntent == Calendar.Intent.Summary)
                {
                    state.Clear();
                }

                if (topIntent == Calendar.Intent.ShowNext)
                {
                    if ((state.ShowEventIndex + 1) * CalendarSkillState.PageSize < state.SummaryEvents.Count)
                    {
                        state.ShowEventIndex++;
                    }
                    else
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.CalendarNoMoreEvent));
                        return await sc.CancelAllDialogsAsync();
                    }
                }

                if (topIntent == Calendar.Intent.ShowPrevious)
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

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> ShowEventsSummary(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var tokenResponse = sc.Result as TokenResponse;

                var state = await _accessor.GetAsync(sc.Context);
                if (state.SummaryEvents == null)
                {
                    if (string.IsNullOrEmpty(state.APIToken))
                    {
                        return await sc.EndDialogAsync(true);
                    }

                    var calendarService = _serviceManager.InitCalendarService(state.APIToken, state.EventSource, state.GetUserTimeZone());

                    var searchDate = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, state.GetUserTimeZone());

                    var startTime = new DateTime(searchDate.Year, searchDate.Month, searchDate.Day);
                    var endTime = new DateTime(searchDate.Year, searchDate.Month, searchDate.Day, 23, 59, 59);
                    var startTimeUtc = TimeZoneInfo.ConvertTimeToUtc(startTime, state.GetUserTimeZone());
                    var endTimeUtc = TimeZoneInfo.ConvertTimeToUtc(endTime, state.GetUserTimeZone());
                    var rawEvents = await calendarService.GetEventsByTime(startTimeUtc, endTimeUtc);
                    var todayEvents = new List<EventModel>();
                    foreach (var item in rawEvents)
                    {
                        if (item.StartTime > searchDate && item.StartTime >= startTime && item.IsCancelled != true)
                        {
                            todayEvents.Add(item);
                        }
                    }

                    if (todayEvents.Count == 0)
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowNoMeetingMessage));
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        var speakParams = new StringDictionary()
                        {
                            { "Count", todayEvents.Count.ToString() },
                            { "EventName1", todayEvents[0].Title },
                            { "EventDuration", todayEvents[0].ToDurationString() },
                        };

                        if (todayEvents.Count == 1)
                        {
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowOneMeetingSummaryMessage, _responseBuilder, speakParams));
                        }
                        else
                        {
                            speakParams.Add("EventName2", todayEvents[todayEvents.Count - 1].Title);
                            speakParams.Add("EventTime", todayEvents[todayEvents.Count - 1].StartTime.ToString("h:mm tt"));
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowOneMeetingSummaryMessage, _responseBuilder, speakParams));
                        }
                    }

                    await ShowMeetingList(sc, todayEvents.GetRange(0, Math.Min(CalendarSkillState.PageSize, todayEvents.Count)), false);
                    state.SummaryEvents = todayEvents;
                }
                else
                {
                    await ShowMeetingList(sc, state.SummaryEvents.GetRange(state.ShowEventIndex * CalendarSkillState.PageSize, Math.Min(CalendarSkillState.PageSize, state.SummaryEvents.Count - (state.ShowEventIndex * CalendarSkillState.PageSize))), false);
                }

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> PromptToRead(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.ReadOutPrompt) });
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> CallReadEventDialog(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.Read);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> ReadEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                var eventItem = state.ReadOutEvents.FirstOrDefault();
                if (topIntent == Luis.Calendar.Intent.ConfirmNo || topIntent == Luis.Calendar.Intent.Reject)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync(true);
                }
                else if (topIntent == Luis.Calendar.Intent.ReadAloud && eventItem == null)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.ReadOutPrompt), });
                }
                else if (eventItem != null)
                {
                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(SummaryResponses.ReadOutMessage, eventItem.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", eventItem.ToAdaptiveCardData());
                    await sc.Context.SendActivityAsync(replyMessage);

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.ReadOutMorePrompt) });
                }
                else
                {
                    return await sc.NextAsync();
                }
            }
            catch (Exception)
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterReadOutEvent(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await _accessor.GetAsync(sc.Context);
                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                if (topIntent == Luis.Calendar.Intent.ReadAloud)
                {
                    return await sc.BeginDialogAsync(Actions.Read);
                }
                else
                {
                    return await sc.EndDialogAsync("true");
                }
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }
    }
}
