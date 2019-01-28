// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Resources;

namespace CalendarSkill.Dialogs.JoinEvent.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class JoinEventResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string NoMeetingTimeProvided = "NoMeetingTimeProvided";
		public const string MeetingNotFound = "MeetingNotFound";
		public const string NoDialInNumber = "NoDialInNumber";
		public const string CallingIn = "CallingIn";

    }
}