using System.Collections.Generic;
using System.Threading.Tasks;
using ITSMSkill.Models;

namespace ITSMSkill.Services
{
    public interface IITServiceManagement
    {
        Task<CreateTicketResult> CreateTicket(string description, UrgencyLevel urgency);

        // like description & in urgencies & equal id & in states
        Task<SearchTicketResult> SearchTicket(string description = null, List<UrgencyLevel> urgencies = null, string id = null, List<TicketState> states = null);

        Task<SearchKnowledgeResult> SearchKnowledge(string query);
    }
}
