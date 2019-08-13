// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace ITSMSkill.Responses.Ticket
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class TicketResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string TicketCreated = "TicketCreated";
        public const string UpdateAttribute = "UpdateAttribute";
        public const string UpdateAttributeMore = "UpdateAttributeMore";
        public const string TicketUpdated = "TicketUpdated";
        public const string ShowConstraintNone = "ShowConstraintNone";
        public const string ShowConstraints = "ShowConstraints";
        public const string ShowAttribute = "ShowAttribute";
        public const string ShowAttributeMore = "ShowAttributeMore";
        public const string TicketShow = "TicketShow";
        public const string TicketShowNone = "TicketShowNone";
    }
}