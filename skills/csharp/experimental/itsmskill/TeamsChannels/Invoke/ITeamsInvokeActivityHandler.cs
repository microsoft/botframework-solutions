namespace ITSMSkill.TeamsChannels.Invoke
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder;

    public interface ITeamsInvokeActivityHandler<T>
    {
        Task<T> Handle(ITurnContext context, CancellationToken cancellationToken);
    }
}
