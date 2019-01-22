using System;
using System.Globalization;
using CalendarSkill.Dialogs.Shared.Resources.Strings;
using Microsoft.Bot.Solutions.Resources;

namespace CalendarSkill.Util
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
                    string result = timeSpan.Hours == 1 ?
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
    }
}
