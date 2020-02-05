// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ITSMSkill.Responses.Ticket
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class TicketResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string TicketCreated = "TicketCreated";
        public const string ConfirmUpdateAttribute = "ConfirmUpdateAttribute";
        public const string UpdateAttribute = "UpdateAttribute";
        public const string TicketUpdated = "TicketUpdated";
        public const string TicketNoUpdate = "TicketNoUpdate";
        public const string ShowConstraintNone = "ShowConstraintNone";
        public const string ShowConstraints = "ShowConstraints";
        public const string ShowUpdateNone = "ShowUpdateNone";
        public const string ShowUpdates = "ShowUpdates";
        public const string ShowAttribute = "ShowAttribute";
        public const string TicketShow = "TicketShow";
        public const string TicketEnd = "TicketEnd";
        public const string TicketShowNone = "TicketShowNone";
        public const string TicketFindNone = "TicketFindNone";
        public const string TicketDuplicateNumber = "TicketDuplicateNumber";
        public const string TicketTarget = "TicketTarget";
        public const string TicketAlreadyClosed = "TicketAlreadyClosed";
        public const string TicketClosed = "TicketClosed";
    }
}