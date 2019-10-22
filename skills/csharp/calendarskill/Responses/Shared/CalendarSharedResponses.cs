// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Responses.Shared
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CalendarSharedResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ActionEnded = "ActionEnded";
        public const string CalendarErrorMessage = "CalendarErrorMessage";
        public const string CalendarErrorMessageBotProblem = "CalendarErrorMessageBotProblem";
        public const string ConfirmMeetingRoomFailed = "ConfirmMeetingRoomFailed";
        public const string ConfirmMeetingRoomPrompt = "ConfirmMeetingRoomPrompt";
        public const string DidntUnderstandMessage = "DidntUnderstandMessage";
        public const string MeetingRoomCreated = "MeetingRoomCreated";
        public const string MultipleEventsFound = "MultipleEventsFound";
        public const string NoBuilding = "NoBuilding";
        public const string NoFloorNumber = "NoFloorNumber";
        public const string NoInvitees = "NoInvitees";
        public const string NoSubject = "NoSubject";
        public const string RetryInput = "RetryInput";
        public const string EventDetails = "EventDetails";
        public const string ConfirmCreatePrompt = "ConfirmCreatePrompt";
    }
}
