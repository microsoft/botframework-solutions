// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill
{
    /// <summary>
    /// Calendar skill actions.
    /// </summary>
    public static class Action
    {
        /// <summary>
        /// Login.
        /// </summary>
        public const string Login = "login";

        /// <summary>
        /// Prompt.
        /// </summary>
        public const string Prompt = "prompt";

        /// <summary>
        /// Choice.
        /// </summary>
        public const string Choice = "choice";

        /// <summary>
        /// Choice for event.
        /// </summary>
        public const string EventChoice = "event_choice";

        /// <summary>
        /// Show events summary.
        /// </summary>
        public const string ShowEventsSummary = "showEventsSummary";

        /// <summary>
        /// Show next event.
        /// </summary>
        public const string ShowNextEvent = "showNextEvent";

        /// <summary>
        /// Create event.
        /// </summary>
        public const string CreateEvent = "createEvent";

        /// <summary>
        /// Update event time by startTime or title.
        /// </summary>
        public const string UpdateEventTime = "UpdateEventTime";

        /// <summary>
        /// Delete event by startTime or title.
        /// </summary>
        public const string DeleteEvent = "DeleteEvent";

        /// <summary>
        /// Update address.
        /// </summary>
        public const string UpdateAddress = "UpdateAddress";

        /// <summary>
        /// Update Name.
        /// </summary>
        public const string UpdateName = "UpdateName";

        /// <summary>
        /// Confirm attendee.
        /// </summary>
        public const string ConfirmAttendee = "ConfirmAttendee";

        /// <summary>
        /// Take further action.
        /// </summary>
        public const string TakeFurtherAction = "TakeFurtherAction";

        /// <summary>
        /// Update start time.
        /// </summary>
        public const string UpdateStartTime = "UpdateStartTime";

        /// <summary>
        /// Update new start time.
        /// </summary>
        public const string UpdateNewStartTime = "UpdateNewStartTime";

        /// <summary>
        /// Update start date for create.
        /// </summary>
        public const string UpdateStartDateForCreate = "UpdateStartDateForCreate";

        /// <summary>
        /// Update start time for create.
        /// </summary>
        public const string UpdateStartTimeForCreate = "UpdateStartTimeForCreate";

        /// <summary>
        /// Update duration for create.
        /// </summary>
        public const string UpdateDurationForCreate = "UpdateDurationForCreate";

        public const string DateTimePrompt = "DateTimePrompt";

        public const string DateTimePromptForUpdateDelete = "DateTimePromptForUpdateDelete";

        public const string Read = "read";

        public const string Greeting = "greeting";
    }
}
