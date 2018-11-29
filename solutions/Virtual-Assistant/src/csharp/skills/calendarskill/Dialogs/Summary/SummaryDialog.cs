using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Common;
using CalendarSkill.Dialogs.Shared.Resources;
using CalendarSkill.Dialogs.Summary.Resources;
using CalendarSkill.ServiceClients;
using CalendarSkill.Util;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill
{
    public class SummaryDialog : CalendarSkillDialog
    {
        public SummaryDialog(
            ISkillConfiguration services,
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
                var state = await Accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;

                var topIntent = luisResult?.TopIntent().intent;

                if (topIntent == Calendar.Intent.Summary || topIntent == Calendar.Intent.FindCalendarEntry)
                {
                    state.SummaryEvents = null;
                }

                var generalLuisResult = state.GeneralLuisResult;
                var topGeneralIntent = generalLuisResult?.TopIntent().intent;

                if (topGeneralIntent == General.Intent.Next)
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

                if (topGeneralIntent == General.Intent.Previous)
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

                var state = await Accessor.GetAsync(sc.Context);
                if (state.SummaryEvents == null)
                {
                    // this will lead to error when test
                    if (string.IsNullOrEmpty(state.APIToken))
                    {
                        return await sc.EndDialogAsync(true);
                    }

                    var calendarAPI = GraphClientHelper.GetCalendarService(state.APIToken, state.EventSource, ServiceManager.GetGoogleClient());
                    var calendarService = ServiceManager.InitCalendarService(calendarAPI, state.EventSource);

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
                        return await sc.EndDialogAsync(true);
                    }
                    else
                    {
                        var responseParams = new StringDictionary()
                        {
                            { "Count", searchedEvents.Count.ToString() },
                            { "EventName1", searchedEvents[0].Title },
                            { "EventDuration", searchedEvents[0].ToDurationString() },
                        };

                        if (searchedEvents.Count == 1)
                        {
                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowOneMeetingSummaryMessage, ResponseBuilder, responseParams));
                        }
                        else
                        {
                            responseParams.Add("EventName2", searchedEvents[searchedEvents.Count - 1].Title);
                            responseParams.Add("EventTime", SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(searchedEvents[searchedEvents.Count - 1].StartTime, state.GetUserTimeZone()), searchedEvents[searchedEvents.Count - 1].IsAllDay == true));

                            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SummaryResponses.ShowMultipleMeetingSummaryMessage, ResponseBuilder, responseParams));
                        }
                    }

                    await ShowMeetingList(sc, searchedEvents.GetRange(0, Math.Min(CalendarSkillState.PageSize, searchedEvents.Count)), false);
                    state.Clear();
                    state.SummaryEvents = searchedEvents;
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
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = sc.Context.Activity.CreateReply(SummaryResponses.ReadOutMorePrompt) });
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
                var state = await Accessor.GetAsync(sc.Context);

                var luisResult = state.LuisResult;
                var topIntent = luisResult?.TopIntent().intent;

                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true);
                }

                var eventItem = state.ReadOutEvents.FirstOrDefault();

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var generalLuisResult = state.GeneralLuisResult;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(CalendarSharedResponses.CancellingMessage));
                    return await sc.EndDialogAsync(true);
                }
                else if ((promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true) || (topIntent == Luis.Calendar.Intent.ReadAloud && eventItem == null))
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

                if (eventItem != null)
                {
                    var speakString = SpeakHelper.ToSpeechMeetingDetail(eventItem.Title, TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()), eventItem.IsAllDay == true);

                    var replyMessage = sc.Context.Activity.CreateAdaptiveCardReply(SummaryResponses.ReadOutMessage, eventItem.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", eventItem.ToAdaptiveCardData(state.GetUserTimeZone()), null, new StringDictionary() { { "MeetingDetails", speakString } });
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
                var state = await Accessor.GetAsync(sc.Context);
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
