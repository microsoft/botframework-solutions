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
        public const string BuildingNonexistent = "BuildingNonexistent";
        public const string NoFloorNumber = "NoFloorNumber";
        public const string FloorNumberRetry = "FloorNumberRetry";
        public const string NoInvitees = "NoInvitees";
        public const string NoSubject = "NoSubject";
        public const string CalendarErrorMessageAccountProblem = "CalendarErrorMessageAccountProblem";
        public const string RetryInput = "RetryInput";
        public const string EventDetails = "EventDetails";
        public const string ConfirmBeforCreatePrompt = "ConfirmBeforCreatePrompt";
        public const string FindNewMeetingRoomPrompt = "FindNewMeetingRoomPrompt";
        public const string CancelRequest = "CancelRequest";
        public const string CannotFindMeetingRoom = "CannotFindMeetingRoom";
        public const string IgnoreMeetingRoom = "IgnoreMeetingRoom";
        public const string RejectConfirmMeetingRoom = "RejectConfirmMeetingRoom";
        public const string RecreateMeetingRoom = "RecreateMeetingRoom";
        public const string RecreateMeetingRoomAgain = "RecreateMeetingRoomAgain";
        public const string BookNewMeetingWithRoom = "BookNewMeetingWithRoom";
    }
}
