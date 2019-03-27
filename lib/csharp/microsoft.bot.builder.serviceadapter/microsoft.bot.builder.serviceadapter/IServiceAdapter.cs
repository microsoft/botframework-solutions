using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceAdapter
{
    public interface IServiceAdapter
    {
        Task ProcessAsync(HttpRequest httpRequest, BotCallbackHandler callback, CancellationToken cancellationToken);
    }
}