// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Responses.UpdateEvent
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class UpdateEventResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string NotEventOrganizer = "NotEventOrganizer";
        public const string ConfirmUpdate = "ConfirmUpdate";
        public const string ConfirmUpdateFailed = "ConfirmUpdateFailed";
        public const string EventUpdated = "EventUpdated";
        public const string NoNewTime = "NoNewTime";
        public const string NoNewTimeRetry = "NoNewTimeRetry";
        public const string EventWithStartTimeNotFound = "EventWithStartTimeNotFound";
        public const string NoDeleteStartTime = "NoDeleteStartTime";
        public const string NoUpdateStartTime = "NoUpdateStartTime";
        public const string MultipleEventsStartAtSameTime = "MultipleEventsStartAtSameTime";
    }
}
