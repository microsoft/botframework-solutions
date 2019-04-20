using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface IActivityHandler
    {
        Task<InvokeResponse> ProcessActivityAsync(Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken);
    }
}