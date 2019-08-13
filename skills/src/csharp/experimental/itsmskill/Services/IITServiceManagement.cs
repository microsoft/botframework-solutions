using System.Collections.Generic;
using System.Threading.Tasks;
using ITSMSkill.Models;

namespace ITSMSkill.Services
{
    public interface IITServiceManagement
    {
        Task<TicketsResult> CreateTicket(string description, UrgencyLevel urgency);

        // like description & in urgencies & equal id & in states
        Task<TicketsResult> SearchTicket(string description = null, List<UrgencyLevel> urgencies = null, string id = null, List<TicketState> states = null);

        Task<TicketsResult> UpdateTicket(string id, string description = null, UrgencyLevel urgency = UrgencyLevel.None);

        Task<TicketsResult> CloseTicket(string id, string reason);

        Task<KnowledgesResult> SearchKnowledge(string query);
    }
}
