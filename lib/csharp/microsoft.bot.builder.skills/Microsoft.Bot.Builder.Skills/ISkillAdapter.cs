using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillAdapter
    {
        Task<InvokeResponse> ProcessActivityAsync(string authHeader, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken);


    }
}