using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.TimeRemain.Resources;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;

namespace CalendarSkill
{
    public class TimeRemainingDialog : CalendarSkillDialog
    {
        public TimeRemainingDialog(
            ISkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(nameof(TimeRemainingDialog), services, accessor, serviceManager)
        {
            var timeRemain = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CheckTimeRemain,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowTimeRemaining, timeRemain));

            // Set starting dialog for component
            InitialDialogId = Actions.ShowTimeRemaining;
        }

        public async Task<DialogTurnResult> CheckTimeRemain(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (string.IsNullOrEmpty(state.APIToken))
                {
                    return await sc.EndDialogAsync(true);
                }

                var calendarAPI = GraphClientHelper.GetCalendarService(state.APIToken, state.EventSource, ServiceManager.GetGoogleClient());
                var calendarService = ServiceManager.InitCalendarService(calendarAPI, state.EventSource);

                var eventList = await calendarService.GetUpcomingEvents();
                var nextEventList = new List<EventModel>();
                foreach (var item in eventList)
                {
                    var itemUserTimeZoneTime = TimeZoneInfo.ConvertTime(item.StartTime, TimeZoneInfo.Utc, state.GetUserTimeZone());
                    if (item.IsCancelled != true && nextEventList.Count == 0)
                    {
                        if (state.OrderReference == "next")
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.StartDate != null && itemUserTimeZoneTime.DayOfYear == state.StartDate.Value.DayOfYear)
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.StartTime != null && itemUserTimeZoneTime.DayOfYear == state.StartTime.Value.DayOfYear)
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.Title != null && item.Title.Equals(state.Title, StringComparison.CurrentCultureIgnoreCase))
                        {
                            nextEventList.Add(item);
                        }
                    }
                }

                if (nextEventList.Count == 0)
                {
                    await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowNoMeetingMessage));
                }
                else
                {
                    var userTimeZone = state.GetUserTimeZone();
                    var timeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Local, userTimeZone);
                    var timeDiff = TimeZoneInfo.ConvertTime(nextEventList[0].StartTime, TimeZoneInfo.Utc, userTimeZone) - timeNow;
                    var timeDiffMinutes = (int)timeDiff.TotalMinutes % 60;
                    var timeDiffHours = (int)timeDiff.TotalMinutes / 60;
                    var timeDiffDays = timeDiff.Days;
                    if (state.OrderReference == "next")
                    {
                        var tokens = new StringDictionary()
                        {
                            { "RemainingHours", timeDiffHours > 1 ? $"{timeDiffHours.ToString()} hours" : $"{timeDiffHours.ToString()} hour" },
                            { "RemainingMinutes", timeDiffMinutes > 1 ? $"{timeDiffMinutes.ToString()} minutes" : $"{timeDiffMinutes.ToString()} minute" }
                        };
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowNextMeetingTimeRemainingMessage, ResponseBuilder, tokens));
                    }
                    else if (state.StartDate != null)
                    {
                        var tokens = new StringDictionary()
                        {
                            { "RemainingDays", timeDiffDays > 1 ? $"{timeDiffDays.ToString()} days" : $"{timeDiffDays.ToString()} day" },
                            { "DateSpeak", state.StartDate.Value.ToSpeechDateString() },
                            { "Date", state.StartDate.Value.ToShortDateString() }
                        };
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowSpecificDaysRemainingMessage, ResponseBuilder, tokens));
                    }
                    else if (state.StartTime != null)
                    {
                        var tokens = new StringDictionary()
                        {
                            { "RemainingHours", timeDiffHours > 1 ? $"{timeDiffHours.ToString()} hours" : $"{timeDiffHours.ToString()} hour" },
                            { "RemainingMinutes", timeDiffMinutes > 1 ? $"{timeDiffMinutes.ToString()} minutes" : $"{timeDiffMinutes.ToString()} minute" },
                            { "TimeSpeak", state.StartTime.Value.ToSpeechTimeString() },
                            { "Time", state.StartTime.Value.ToShortTimeString() }
                        };
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowSpecificTimeRemainingMessage, ResponseBuilder, tokens));
                    }
                    else if (state.Title != null)
                    {
                        var tokens = new StringDictionary()
                        {
                            { "RemainingHours", timeDiffHours > 1 ? $"{timeDiffHours.ToString()} hours" : $"{timeDiffHours.ToString()} hour" },
                            { "RemainingMinutes", timeDiffMinutes > 1 ? $"{timeDiffMinutes.ToString()} minutes" : $"{timeDiffMinutes.ToString()} minute" },
                            { "Title", state.Title }
                        };
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowSpecificTitleRemainingMessage, ResponseBuilder, tokens));
                    }
                }

                state.Clear();
                return await sc.EndDialogAsync(true);
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }
    }
}
