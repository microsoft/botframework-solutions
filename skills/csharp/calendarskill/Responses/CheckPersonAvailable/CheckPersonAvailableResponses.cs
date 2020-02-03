// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Responses.CheckPersonAvailable
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CheckPersonAvailableResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string AskForCheckAvailableTime = "AskForCheckAvailableTime";
        public const string AskForCheckAvailableUserName = "AskForCheckAvailableUserName";
        public const string NotAvailable = "NotAvailable";
        public const string AttendeeIsAvailable = "AttendeeIsAvailable";
        public const string AttendeeIsAvailableOrgnizerIsUnavailableWithOneConflict = "AttendeeIsAvailableOrgnizerIsUnavailableWithOneConflict";
        public const string AttendeeIsAvailableOrgnizerIsUnavailableWithMutipleConflicts = "AttendeeIsAvailableOrgnizerIsUnavailableWithMutipleConflicts";
        public const string AskForCreateNewMeeting = "AskForCreateNewMeeting";
        public const string AskForCreateNewMeetingAnyway = "AskForCreateNewMeetingAnyway";
        public const string AskForNextAvailableTime = "AskForNextAvailableTime";
        public const string NextBothAvailableTime = "NextBothAvailableTime";
        public const string NoNextBothAvailableTime = "NoNextBothAvailableTime";
    }
}
