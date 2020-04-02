namespace ITSMSkill.Dialogs.Teams.TicketTaskModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.TeamsChannels;
    using ITSMSkill.TeamsChannels.Invoke;
    using Microsoft.Bot.Builder;

    [TeamsInvoke(FlowType = nameof(TeamsFlowType.UpdateTicket_Form))]
    public class UpdateTicketTaskModuleHandler : ITeamsInvokeActivityHandler<TaskEnvelope>
    {
        public UpdateTicketTaskModuleHandler(IServiceProvider serviceProvider)
        {

        }

        public Task<TaskEnvelope> Handle(ITurnContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
