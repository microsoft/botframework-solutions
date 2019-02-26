﻿// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Dialogs.UpcomingEvent.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class UpcomingEventResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string UpcomingEventMessage = "UpcomingEventMessage";
        public const string UpcomingEventMessageWithLocation = "UpcomingEventMessageWithLocation";
    }
}