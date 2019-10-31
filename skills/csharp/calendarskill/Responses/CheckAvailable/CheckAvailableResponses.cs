// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace CalendarSkill.Responses.CheckAvailable
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CheckAvailableResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string AskForCheckAvailableTime = "AskForCheckAvailableTime";
        public const string NotAvailable = "NotAvailable";
    }
}
