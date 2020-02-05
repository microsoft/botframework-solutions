// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Responses.TimeRemaining
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class TimeRemainingResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ShowNextMeetingTimeRemainingMessage = "ShowNextMeetingTimeRemainingMessage";
        public const string ShowTimeRemainingMessage = "ShowTimeRemainingMessage";
        public const string ShowNoMeetingMessage = "ShowNoMeetingMessage";
    }
}
