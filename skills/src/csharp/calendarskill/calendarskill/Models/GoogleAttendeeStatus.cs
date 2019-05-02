// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill.Models
{
    public static class GoogleAttendeeStatus
    {
        /// <summary>
        /// Event is not responded yet.
        /// </summary>
        public const string NeedsAction = "needsAction";

        /// <summary>
        /// Event is declined.
        /// </summary>
        public const string Declined = "declined";

        /// <summary>
        /// Event is tentative.
        /// </summary>
        public const string Tentative = "tentative";

        /// <summary>
        /// Event is accepted.
        /// </summary>
        public const string Accepted = "accepted";
    }
}
