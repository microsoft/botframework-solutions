// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Shared.Responses;

namespace CalendarSkill.Responses.Summary
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class SummaryResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string CalendarNoMoreEvent = "CalendarNoMoreEvent";
        public const string CalendarNoPreviousEvent = "CalendarNoPreviousEvent";
        public const string ShowNoMeetingMessage = "ShowNoMeetingMessage";
        public const string ShowOneMeetingSummaryMessage = "ShowOneMeetingSummaryMessage";
        public const string ShowMultipleMeetingSummaryMessage = "ShowMultipleMeetingSummaryMessage";
        public const string ShowOneMeetingSummaryAgainMessage = "ShowOneMeetingSummaryAgainMessage";
        public const string ShowMeetingSummaryAgainMessage = "ShowMeetingSummaryAgainMessage";
        public const string ShowMeetingSummaryNotFirstPageMessage = "ShowMeetingSummaryNotFirstPageMessage";
        public const string ShowMultipleFilteredMeetings = "ShowMultipleFilteredMeetings";
        public const string ReadOutPrompt = "ReadOutPrompt";
        public const string ReadOutMorePrompt = "ReadOutMorePrompt";
        public const string ReadOutMessage = "ReadOutMessage";
        public const string ShowNextMeetingNoLocationMessage = "ShowNextMeetingNoLocationMessage";
        public const string ShowNextMeetingMessage = "ShowNextMeetingMessage";
        public const string ShowMultipleNextMeetingMessage = "ShowMultipleNextMeetingMessage";
        public const string BeforeShowEventDetails = "BeforeShowEventDetails";
        public const string ReadTime = "ReadTime";
        public const string ReadDuration = "ReadDuration";
        public const string ReadLocation = "ReadLocation";
        public const string ReadNoLocation = "ReadNoLocation";
        public const string AskForChangeStatus = "AskForChangeStatus";
        public const string AskForAction = "AskForAction";
        public const string AskForOrgnizerAction = "AskForOrgnizerAction";
        public const string AskForShowOverview = "AskForShowOverview";
    }
}
