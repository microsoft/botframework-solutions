// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Dialogs.ChangeEventStatus.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ChangeEventStatusResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ConfirmDelete = "ConfirmDelete";
        public const string ConfirmDeleteFailed = "ConfirmDeleteFailed";
        public const string ConfirmAccept = "ConfirmAccept";
        public const string ConfirmAcceptFailed = "ConfirmAcceptFailed";
        public const string EventDeleted = "EventDeleted";
        public const string EventAccepted = "EventAccepted";
        public const string EventWithStartTimeNotFound = "EventWithStartTimeNotFound";
        public const string NoDeleteStartTime = "NoDeleteStartTime";
        public const string NoAcceptStartTime = "NoAcceptStartTime";
        public const string MultipleEventsStartAtSameTime = "MultipleEventsStartAtSameTime";
    }
}