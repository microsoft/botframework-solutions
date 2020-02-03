// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;
using Luis;

namespace CalendarSkill.Utilities
{
    public class CalendarCommonUtil
    {
        public const int MaxDisplaySize = 3;

        public const int MaxRepromptCount = 3;

        public const int AvailabilityViewInterval = 5;


        public static async Task<List<EventModel>> GetEventsByTime(List<DateTime> startDateList, List<DateTime> startTimeList, List<DateTime> endDateList, List<DateTime> endTimeList, TimeZoneInfo userTimeZone, ICalendarService calendarService)
        {
            // todo: check input datetime is utc
            var rawEvents = new List<EventModel>();
            var resultEvents = new List<EventModel>();

            if (!startDateList.Any() && !startTimeList.Any() && !endDateList.Any() && !endTimeList.Any())
            {
                return resultEvents;
            }

            DateTime? startDate = null;
            if (startDateList.Any())
            {
                startDate = startDateList.Last();
            }

            DateTime? endDate = null;
            if (endDateList.Any())
            {
                endDate = endDateList.Last();
            }

            var searchByStartTime = startTimeList.Any() && endDate == null && !endTimeList.Any();

            startDate = startDate ?? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);
            endDate = endDate ?? startDate ?? TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);

            var searchStartTimeList = new List<DateTime>();
            var searchEndTimeList = new List<DateTime>();

            if (startTimeList.Any())
            {
                foreach (var time in startTimeList)
                {
                    searchStartTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                        new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day, time.Hour, time.Minute, time.Second),
                        userTimeZone));
                }
            }
            else
            {
                searchStartTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(startDate.Value.Year, startDate.Value.Month, startDate.Value.Day), userTimeZone));
            }

            if (endTimeList.Any())
            {
                foreach (var time in endTimeList)
                {
                    searchEndTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                        new DateTime(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, time.Hour, time.Minute, time.Second),
                        userTimeZone));
                }
            }
            else
            {
                searchEndTimeList.Add(TimeZoneInfo.ConvertTimeToUtc(
                    new DateTime(endDate.Value.Year, endDate.Value.Month, endDate.Value.Day, 23, 59, 59), userTimeZone));
            }

            DateTime? searchStartTime = null;

            if (searchByStartTime)
            {
                foreach (var startTime in searchStartTimeList)
                {
                    rawEvents = await calendarService.GetEventsByStartTimeAsync(startTime);
                    if (rawEvents.Any())
                    {
                        searchStartTime = startTime;
                        break;
                    }
                }
            }
            else
            {
                for (var i = 0; i < searchStartTimeList.Count(); i++)
                {
                    rawEvents = await calendarService.GetEventsByTimeAsync(
                        searchStartTimeList[i],
                        searchEndTimeList.Count() > i ? searchEndTimeList[i] : searchEndTimeList[0]);
                    if (rawEvents.Any())
                    {
                        searchStartTime = searchStartTimeList[i];
                        break;
                    }
                }
            }

            foreach (var item in rawEvents)
            {
                if (item.StartTime >= searchStartTime && item.IsCancelled != true)
                {
                    resultEvents.Add(item);
                }
            }

            return resultEvents;
        }

        public static bool ContainsTime(string timex)
        {
            return timex.Contains("T");
        }

        public static CalendarLuis.Intent CheckIntentSwitching(CalendarLuis.Intent intent)
        {
            switch (intent)
            {
                case CalendarLuis.Intent.AcceptEventEntry:
                case CalendarLuis.Intent.ChangeCalendarEntry:
                case CalendarLuis.Intent.CheckAvailability:
                case CalendarLuis.Intent.ConnectToMeeting:
                case CalendarLuis.Intent.CreateCalendarEntry:
                case CalendarLuis.Intent.DeleteCalendarEntry:
                case CalendarLuis.Intent.FindCalendarDetail:
                case CalendarLuis.Intent.FindCalendarEntry:
                case CalendarLuis.Intent.FindCalendarWhen:
                case CalendarLuis.Intent.FindCalendarWhere:
                case CalendarLuis.Intent.FindCalendarWho:
                case CalendarLuis.Intent.FindDuration:
                case CalendarLuis.Intent.TimeRemaining:
                    return intent;
                default:
                    return CalendarLuis.Intent.None;
            }
        }

        public static bool IsFindEventsDialog(CalendarLuis.Intent intent)
        {
            switch (intent)
            {
                case CalendarLuis.Intent.FindCalendarDetail:
                case CalendarLuis.Intent.FindCalendarEntry:
                case CalendarLuis.Intent.FindCalendarWhen:
                case CalendarLuis.Intent.FindCalendarWhere:
                case CalendarLuis.Intent.FindCalendarWho:
                case CalendarLuis.Intent.FindDuration:
                    return true;
                default:
                    return false;
            }
        }

        public static bool ContainMeetingRoomSlot(CalendarLuis luis)
        {
            return ContainSlot(luis, SlotNames.Room);
        }

        public static bool ContainTimeSlot(CalendarLuis luis)
        {
            return ContainSlot(luis, SlotNames.Time);
        }

        /*
         SlotAttributeName is entity list, while SlotAttribute is simple entity. SlotAttributeName is the canonical form of SlotAttribute.
         When both SlotAttributeName and SlotAttribute are recognized, this entity is confirmed.
         */
        private static bool ContainSlot(CalendarLuis luis, string slotName)
        {
            if (luis == null || luis.Entities == null || luis.Entities.SlotAttributeName == null || luis.Entities.SlotAttribute == null)
            {
                return false;
            }

            for (int i = 0; i < luis.Entities.SlotAttributeName.Length; i++)
            {
                for (int j = 0; j < luis.Entities.SlotAttribute.Length; j++)
                {
                    if (luis.Entities.SlotAttributeName[i][0] == slotName &&
                        luis.Entities._instance.SlotAttributeName[i].Text == luis.Entities._instance.SlotAttribute[j].Text &&
                        luis.Entities._instance.SlotAttributeName[i].StartIndex == luis.Entities._instance.SlotAttribute[j].StartIndex &&
                        luis.Entities._instance.SlotAttributeName[i].EndIndex == luis.Entities._instance.SlotAttribute[j].EndIndex)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private class SlotNames
        {
            public const string Time = "time";
            public const string Room = "room";
        }
    }
}
