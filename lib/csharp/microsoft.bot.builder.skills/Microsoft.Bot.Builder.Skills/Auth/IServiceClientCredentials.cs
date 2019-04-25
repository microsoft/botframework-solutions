using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Auth
{
    public interface IServiceClientCredentials
    {
        Task<string> GetTokenAsync(bool forceRefresh = false);

        Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}