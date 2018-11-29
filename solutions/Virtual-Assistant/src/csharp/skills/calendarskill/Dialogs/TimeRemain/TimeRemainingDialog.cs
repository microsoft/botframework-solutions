using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
                        else if (state.StartDate.Any() && itemUserTimeZoneTime.DayOfYear == state.StartDate[0].DayOfYear)
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.StartTime.Any() && itemUserTimeZoneTime.DayOfYear == state.StartTime[0].DayOfYear)
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

                    var tokens = new StringDictionary()
                    {
                        { "RemainingTime", string.Empty },
                        { "Title", string.Empty },
                        { "Time", string.Empty},
                        { "TimeSpeak", string.Empty }
                    };

                    var remainingMinutes = string.Empty;
                    var remainingHours = string.Empty;
                    var remainingDays = string.Empty;

                    if (timeDiffMinutes > 0)
                    {
                        if (timeDiffMinutes > 1)
                        {
                            remainingMinutes = $"{timeDiffMinutes.ToString()} minutes ";
                        }
                        else
                        {
                            remainingMinutes = $"{timeDiffMinutes.ToString()} minute ";
                        }
                    }

                    if (timeDiffHours > 0)
                    {
                        if (timeDiffHours > 1)
                        {
                            remainingHours = $"{timeDiffHours.ToString()} hours ";
                        }
                        else
                        {
                            remainingHours = $"{timeDiffHours.ToString()} hour ";
                        }
                    }

                    if (timeDiffDays > 0)
                    {
                        if (timeDiffHours > 1)
                        {
                            remainingDays = $"{timeDiffDays.ToString()} days ";
                        }
                        else
                        {
                            remainingDays = $"{timeDiffDays.ToString()} day ";
                        }
                    }

                    var remainingTime = $"{remainingDays}{remainingHours}{remainingMinutes}";
                    tokens["RemainingTime"] = remainingTime;
                    if (state.OrderReference == "next")
                    {
                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowNextMeetingTimeRemainingMessage, ResponseBuilder, tokens));
                    }
                    else
                    {
                        var timeToken = string.Empty;
                        var timeSpeakToken = string.Empty;

                        if (state.StartDate.Any())
                        {
                            timeSpeakToken += $"{state.StartDate[0].ToSpeechDateString()} ";
                            timeToken += $"{state.StartDate[0].ToShortDateString()} ";
                        }

                        if (state.StartTime.Any())
                        {
                            timeSpeakToken += $"{state.StartTime[0].ToSpeechTimeString()}";
                            timeToken += $"{state.StartTime[0].ToShortTimeString()}";
                        }

                        if (timeSpeakToken.Length > 0)
                        {
                            tokens["TimeSpeak"] = $"at {timeSpeakToken}";
                        }

                        if (timeToken.Length > 0)
                        {
                            tokens["Time"] = $"at {timeToken}";
                        }

                        if (state.Title != null)
                        {
                            tokens["Title"] = $"with a subject of \"{state.Title}\"";
                        }

                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowTimeRemainingMessage, ResponseBuilder, tokens));
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
