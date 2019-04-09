using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Shared
{
    /// <summary>
    /// Interface that represents remove invocation behavior.
    /// </summary>
    public interface IRemoteUserTokenProvider
    {
        Task SendRemoteTokenRequestEvent(ITurnContext turnContext, CancellationToken cancellationToken);
    }
}