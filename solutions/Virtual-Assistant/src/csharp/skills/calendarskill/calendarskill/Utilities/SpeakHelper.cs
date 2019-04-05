﻿using System;
using System.Collections.Generic;
using System.Globalization;
using CalendarSkill.Responses.Shared;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;

namespace CalendarSkill.Utilities
{
    public class SpeakHelper
    {
        public static string ToSpeechMeetingDateTime(DateTime dateTime, bool isAllDay)
        {
            // All day: "{0} all day"
            // Time: "h:mm tt"
            if (dateTime == null)
            {
                return string.Empty;
            }

            if (isAllDay)
            {
                return string.Format(CommonStrings.DateWithAllDay, dateTime.ToString(CommonStrings.SpokenDateFormat, CultureInfo.CurrentUICulture));
            }
            else
            {
                return dateTime.ToString(CommonStrings.DisplayTime);
            }
        }

        public static string ToSpeechMeetingTime(DateTime dateTime, bool isAllDay)
        {
            // All day: "all day"
            // Time: "at h:mm tt"
            if (dateTime == null)
            {
                return string.Empty;
            }

            if (isAllDay)
            {
                return CalendarCommonStrings.AllDayLower;
            }
            else
            {
                return string.Format(CalendarCommonStrings.AtTime, dateTime.ToString(CommonStrings.DisplayTime));
            }
        }

        public static string ToSpeechMeetingDetail(string title, DateTime dateTime, bool isAllDay)
        {
            // {title} at {time}
            if (title == null || dateTime == null)
            {
                return string.Empty;
            }

            return string.Format(CommonStrings.AtTimeDetailsFormat, title, ToSpeechMeetingDateTime(dateTime, isAllDay));
        }

        public static string ToSpeechMeetingDuration(TimeSpan timeSpan)
        {
            if (timeSpan == null)
            {
                return string.Empty;
            }

            if (timeSpan.TotalHours < 1)
            {
                return timeSpan.Minutes == 1 ?
                    string.Format(CommonStrings.TimeFormatMinute, timeSpan.Minutes) :
                    string.Format(CommonStrings.TimeFormatMinutes, timeSpan.Minutes);
            }
            else if (timeSpan.TotalDays < 1)
            {
                if (timeSpan.Minutes == 0)
                {
                    return timeSpan.Hours == 1 ?
                        string.Format(CommonStrings.TimeFormatHour, timeSpan.Hours) :
                        string.Format(CommonStrings.TimeFormatHours, timeSpan.Hours);
                }
                else
                {
                    var result = timeSpan.Hours == 1 ?
                        string.Format(CommonStrings.TimeFormatHour, timeSpan.Hours) :
                        string.Format(CommonStrings.TimeFormatHours, timeSpan.Hours);
                    result += CommonStrings.TimeFormatHourMinuteConnective;
                    result += timeSpan.Minutes == 1 ?
                        string.Format(CommonStrings.TimeFormatMinute, timeSpan.Minutes) :
                        string.Format(CommonStrings.TimeFormatMinutes, timeSpan.Minutes);
                    return result;
                }
            }
            else
            {
                return timeSpan.Days == 1 ?
                    string.Format(CommonStrings.TimeFormatDay, timeSpan.Days) :
                    string.Format(CommonStrings.TimeFormatDays, timeSpan.Days);
            }
        }

        public static string ToSpeechSelectionDetailString(PromptOptions selectOption, int maxSize)
        {
            var result = string.Empty;
            result += selectOption.Prompt.Text + "\r\n";

            var selectionDetails = new List<string>();

            var readSize = Math.Min(selectOption.Choices.Count, maxSize);
            if (readSize == 1)
            {
                selectionDetails.Add(selectOption.Choices[0].Value);
            }
            else
            {
                for (var i = 0; i < readSize; i++)
                {
                    var readFormat = string.Empty;

                    if (i == 0)
                    {
                        readFormat = CommonStrings.FirstItem;
                    }
                    else if (i == 1)
                    {
                        readFormat = CommonStrings.SecondItem;
                    }
                    else if (i == 2)
                    {
                        readFormat = CommonStrings.ThirdItem;
                    }

                    var selectionDetail = string.Format(readFormat, selectOption.Choices[i].Value);
                    selectionDetails.Add(selectionDetail);
                }
            }

            result += selectionDetails.ToSpeechString(CommonStrings.And);
            return result;
        }
    }
}
