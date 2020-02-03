// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Responses.FindMeetingRoom
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class FindMeetingRoomResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ConfirmMultipleMeetingRoomMultiPage = "ConfirmMultipleMeetingRoomMultiPage";
        public const string ConfirmMultipleMeetingRoomSinglePage = "ConfirmMultipleMeetingRoomSinglePage";
        public const string FindMeetingRoomNoAttendees = "FindMeetingRoomNoAttendees";
        public const string FindMeetingRoomNoDuration = "FindMeetingRoomNoDuration";
        public const string FindMeetingRoomNoDurationRetry = "FindMeetingRoomNoDurationRetry";
        public const string FindMeetingRoomNoTitle = "FindMeetingRoomNoTitle";
        public const string FindMeetingRoomNoTitleShort = "FindMeetingRoomNoTitleShort";
        public const string MeetingRoomNotFoundByName = "MeetingRoomNotFoundByName";
        public const string MeetingRoomUnavailable = "MeetingRoomUnavailable";
        public const string MeetingRoomNotFoundByBuildingAndFloor = "MeetingRoomNotFoundByBuildingAndFloor";
        public const string CannotFindOtherMeetingRoom = "CannotFindOtherMeetingRoom";
        public const string IgnoreMeetingRoom = "IgnoreMeetingRoom";
        public const string RejectConfirmMeetingRoom = "RejectConfirmMeetingRoom";
        public const string RecreateMeetingRoom = "RecreateMeetingRoom";
        public const string RecreateMeetingRoomAgain = "RecreateMeetingRoomAgain";
        public const string BookNewMeetingWithRoom = "BookNewMeetingWithRoom";
        public const string NoBuilding = "NoBuilding";
        public const string BuildingNonexistent = "BuildingNonexistent";
        public const string NoFloorNumber = "NoFloorNumber";
        public const string FloorNumberRetry = "FloorNumberRetry";
        public const string MeetingRoomCreated = "MeetingRoomCreated";
        public const string ConfirmMeetingRoomFailed = "ConfirmMeetingRoomFailed";
        public const string ConfirmMeetingRoomPrompt = "ConfirmMeetingRoomPrompt";
        public const string CancelRequest = "CancelRequest";
        public const string ConfirmedMeeting = "ConfirmedMeeting";
        public const string ConfirmAddMeetingRoom = "ConfirmAddMeetingRoom";
        public const string ConfirmChangeMeetingRoom = "ConfirmChangeMeetingRoom";
        public const string ConfirmBeforCreatePrompt = "ConfirmBeforCreatePrompt";
        public const string ConfirmedMeetingRoom = "ConfirmedMeetingRoom";
        public const string MeetingRoomAdded = "MeetingRoomAdded";
        public const string MeetingRoomChanged = "MeetingRoomChanged";
        public const string MeetingRoomCanceled = "MeetingRoomCanceled";
    }
}
