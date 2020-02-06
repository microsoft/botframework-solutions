using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;

namespace Bot.Builder.Community.Adapters.Google.Integration.AspNet.Core
{
    public interface IGoogleHttpAdapter
    {
        Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default(CancellationToken));
    }
}
