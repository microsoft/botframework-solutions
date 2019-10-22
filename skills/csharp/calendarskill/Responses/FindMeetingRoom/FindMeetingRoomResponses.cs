// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Responses.FindMeetingRoom
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class FindMeetingRoomResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string FindMeetingRoomNoTitle = "FindMeetingRoomNoTitle";
        public const string FindMeetingRoomNoTitleShort = "FindMeetingRoomNoTitleShort";
        public const string FindMeetingRoomNoDuration = "FindMeetingRoomNoDuration";
        public const string FindMeetingRoomNoDurationRetry = "FindMeetingRoomNoDurationRetry";
        public const string MeetingBooked = "MeetingBooked";
        public const string NoMeetingRoom = "NoMeetingRoom";
        public const string AddMoreAttendees = "AddMoreAttendees";
        public const string AddMoreUserPrompt = "AddMoreUserPrompt";
        public const string AlreadyFirstPage = "AlreadyFirstPage";
        public const string AlreadyLastPage = "AlreadyLastPage";
        public const string BeforeSendingMessage = "BeforeSendingMessage";
        public const string ConfirmMultiplContactEmailMultiPage = "ConfirmMultiplContactEmailMultiPage";
        public const string ConfirmMultiplContactEmailSinglePage = "ConfirmMultiplContactEmailSinglePage";
        public const string ConfirmMultipleContactNameMultiPage = "ConfirmMultipleContactNameMultiPage";
        public const string ConfirmMultipleContactNameSinglePage = "ConfirmMultipleContactNameSinglePage";
        public const string ConfirmMultipleMeetingRoomMultiPage = "ConfirmMultipleMeetingRoomMultiPage";
        public const string ConfirmMultipleMeetingRoomSinglePage = "ConfirmMultipleMeetingRoomSinglePage";
        public const string NoAttendees = "NoAttendees";
        public const string PromptOneNameOneAddress = "PromptOneNameOneAddress";
        public const string UserNotFound = "UserNotFound";
        public const string UserNotFoundAgain = "UserNotFoundAgain";
    }
}
