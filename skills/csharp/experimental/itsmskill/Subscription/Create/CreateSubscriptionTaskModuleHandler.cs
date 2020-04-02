namespace ITSMSkill.Subscription.Create
{
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.TeamsChannels;
    using ITSMSkill.TeamsChannels.Invoke;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using Microsoft.Bot.Builder;

    /// <summary>
    /// Handler for Create Subscription Task Module.
    /// </summary>
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.CreateSubscription_Form))]
    public class CreateSubscriptionTaskModuleHandler : ITeamsInvokeActivityHandler<TaskEnvelope>
    {
        public Task<TaskEnvelope> Handle(ITurnContext context, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
