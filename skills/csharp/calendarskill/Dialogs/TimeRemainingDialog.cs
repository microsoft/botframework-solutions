﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.TimeRemaining;
using CalendarSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;

namespace CalendarSkill.Dialogs
{
    public class TimeRemainingDialog : CalendarSkillDialogBase
    {
        public TimeRemainingDialog(
            BotSettings settings,
            BotServices services,
            ResponseManager responseManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            MicrosoftAppCredentials appCredentials)
            : base(nameof(TimeRemainingDialog), settings, services, responseManager, conversationState, serviceManager, telemetryClient, appCredentials)
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

        private async Task<DialogTurnResult> CheckTimeRemain(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                sc.Context.TurnState.TryGetValue(APITokenKey, out var token);

                var calendarService = ServiceManager.InitCalendarService((string)token, state.EventSource);

                var eventList = await calendarService.GetUpcomingEventsAsync();
                var nextEventList = new List<EventModel>();
                foreach (var item in eventList)
                {
                    var itemUserTimeZoneTime = TimeZoneInfo.ConvertTime(item.StartTime, TimeZoneInfo.Utc, state.GetUserTimeZone());
                    if (item.IsCancelled != true && nextEventList.Count == 0)
                    {
                        if (state.MeetingInfor.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.MeetingInfor.StartDate.Any() && itemUserTimeZoneTime.DayOfYear == state.MeetingInfor.StartDate[0].DayOfYear)
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.MeetingInfor.StartTime.Any() && itemUserTimeZoneTime == state.MeetingInfor.StartTime[0])
                        {
                            nextEventList.Add(item);
                        }
                        else if (state.MeetingInfor.Title != null && item.Title.Equals(state.MeetingInfor.Title, StringComparison.CurrentCultureIgnoreCase))
                        {
                            nextEventList.Add(item);
                        }
                    }
                }

                if (nextEventList.Count == 0)
                {
                    var prompt = ResponseManager.GetResponse(TimeRemainingResponses.ShowNoMeetingMessage);
                    await sc.Context.SendActivityAsync(prompt);
                    return await sc.EndDialogAsync();
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
                    if (state.MeetingInfor.OrderReference == "next")
                    {
                        var prompt = ResponseManager.GetResponse(TimeRemainingResponses.ShowNextMeetingTimeRemainingMessage, tokens);
                        await sc.Context.SendActivityAsync(prompt);
                        return await sc.EndDialogAsync();
                    }
                    else
                    {
                        var timeToken = string.Empty;
                        var timeSpeakToken = string.Empty;

                        if (state.MeetingInfor.StartDate.Any())
                        {
                            timeSpeakToken += $"{state.MeetingInfor.StartDate[0].ToSpeechDateString()} ";
                            timeToken += $"{state.MeetingInfor.StartDate[0].ToShortDateString()} ";
                        }

                        if (state.MeetingInfor.StartTime.Any())
                        {
                            timeSpeakToken += $"{state.MeetingInfor.StartTime[0].ToSpeechTimeString()}";
                            timeToken += $"{state.MeetingInfor.StartTime[0].ToShortTimeString()}";
                        }

                        if (timeSpeakToken.Length > 0)
                        {
                            tokens["TimeSpeak"] = CommonStrings.SpokenTimePrefix_One + " " + timeSpeakToken;
                        }

                        if (timeToken.Length > 0)
                        {
                            tokens["Time"] = CommonStrings.SpokenTimePrefix_One + " " + timeToken;
                        }

                        if (state.MeetingInfor.Title != null)
                        {
                            tokens["Title"] = string.Format(CalendarCommonStrings.WithTheSubject, state.MeetingInfor.Title);
                        }

                        var prompt = ResponseManager.GetResponse(TimeRemainingResponses.ShowTimeRemainingMessage, tokens);
                        await sc.Context.SendActivityAsync(prompt);
                        return await sc.EndDialogAsync();
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
    }
}