using ITSMSkill.Extensions.Teams;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.TeamsChannels;
using ITSMSkill.TeamsChannels.Invoke;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ITSMSkill.Dialogs.Teams
{
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.UpdateTicket_Form))]
    public class UpdateTicketTeamsImplementation : ITeamsInvokeActivityHandler<TaskEnvelope>
    {
        public Task<TaskEnvelope> Handle(ITurnContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
