// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using ITSMSkill.Models;

namespace ITSMSkill.Services
{
    public interface IITServiceManagement
    {
        Task<TicketsResult> CreateTicket(string title, string description, UrgencyLevel urgency);

        // like query & in urgencies & equal id & in states
        Task<TicketsResult> SearchTicket(int pageIndex, string query = null, List<UrgencyLevel> urgencies = null, string id = null, List<TicketState> states = null, string number = null);

        // only count. so Tickets are all null
        Task<TicketsResult> CountTicket(string query = null, List<UrgencyLevel> urgencies = null, string id = null, List<TicketState> states = null, string number = null);

        Task<TicketsResult> UpdateTicket(string id, string title = null, string description = null, UrgencyLevel urgency = UrgencyLevel.None);

        Task<TicketsResult> CloseTicket(string id, string reason);

        Task<KnowledgesResult> SearchKnowledge(string query, int pageIndex);

        // only count. so Knowledges are all null
        Task<KnowledgesResult> CountKnowledge(string query);
    }
}
