using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Dialogs.Shared;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using CalendarSkill.Dialogs.TimeRemain.Resources;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace CalendarSkill.Dialogs.TimeRemain
{
    public class TimeRemainingDialog : CalendarSkillDialog
    {
        public TimeRemainingDialog(
            SkillConfigurationBase services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(TimeRemainingDialog), services, accessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var timeRemain = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                CheckTimeRemain,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowTimeRemaining, timeRemain) { TelemetryClient = telemetryClient });

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

                var calendarService = ServiceManager.InitCalendarService(state.APIToken, state.EventSource);

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
                        else if (state.StartTime.Any() && itemUserTimeZoneTime == state.StartTime[0])
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
                        { "Time", string.Empty },
                        { "TimeSpeak", string.Empty }
                    };

                    var remainingMinutes = string.Empty;
                    var remainingHours = string.Empty;
                    var remainingDays = string.Empty;

                    if (timeDiffMinutes > 0)
                    {
                        if (timeDiffMinutes > 1)
                        {
                            remainingMinutes = string.Format(CommonStrings.TimeFormatMinutes, timeDiffMinutes) + " ";
                        }
                        else
                        {
                            remainingMinutes = string.Format(CommonStrings.TimeFormatMinute, timeDiffMinutes) + " ";
                        }
                    }

                    if (timeDiffHours > 0)
                    {
                        if (timeDiffHours > 1)
                        {
                            remainingHours = string.Format(CommonStrings.TimeFormatHours, timeDiffHours) + " ";
                        }
                        else
                        {
                            remainingHours = string.Format(CommonStrings.TimeFormatHour, timeDiffHours) + " ";
                        }
                    }

                    if (timeDiffDays > 0)
                    {
                        if (timeDiffDays > 1)
                        {
                            remainingDays = string.Format(CommonStrings.TimeFormatDays, timeDiffDays) + " ";
                        }
                        else
                        {
                            remainingDays = string.Format(CommonStrings.TimeFormatDay, timeDiffDays) + " ";
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
                            tokens["TimeSpeak"] = CommonStrings.SpokenTimePrefix_One + " " + timeSpeakToken;
                        }

                        if (timeToken.Length > 0)
                        {
                            tokens["Time"] = CommonStrings.SpokenTimePrefix_One + " " + timeToken;
                        }

                        if (state.Title != null)
                        {
                            tokens["Title"] = string.Format(CalendarCommonStrings.WithTheSubject, state.Title);
                        }

                        await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(TimeRemainResponses.ShowTimeRemainingMessage, ResponseBuilder, tokens));
                    }
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
    }
}