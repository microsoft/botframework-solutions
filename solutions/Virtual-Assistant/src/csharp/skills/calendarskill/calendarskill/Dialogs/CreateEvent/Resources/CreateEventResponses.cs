// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Dialogs.CreateEvent.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CreateEventResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string NoTitle = "NoTitle";
		public const string NoTitle_Short = "NoTitle_Short";
		public const string NoContent = "NoContent";
		public const string NoLocation = "NoLocation";
		public const string ConfirmCreate = "ConfirmCreate";
		public const string ConfirmCreateFailed = "ConfirmCreateFailed";
		public const string EventCreated = "EventCreated";
		public const string EventCreationFailed = "EventCreationFailed";
		public const string NoAttendees = "NoAttendees";
		public const string PromptTooManyPeople = "PromptTooManyPeople";
		public const string PromptPersonNotFound = "PromptPersonNotFound";
		public const string NoStartDate = "NoStartDate";
		public const string NoStartDate_Retry = "NoStartDate_Retry";
		public const string NoStartTime = "NoStartTime";
		public const string NoStartTime_Retry = "NoStartTime_Retry";
		public const string NoStartTime_NoSkip = "NoStartTime_NoSkip";
		public const string NoDuration = "NoDuration";
		public const string NoDuration_Retry = "NoDuration_Retry";
		public const string GetRecreateInfo = "GetRecreateInfo";
		public const string GetRecreateInfo_Retry = "GetRecreateInfo_Retry";
		public const string ConfirmRecipient = "ConfirmRecipient";
		public const string InvaildDuration = "InvaildDuration";

    }
}