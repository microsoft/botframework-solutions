using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceAdapter
{
    public interface IServiceAdapter
    {
        Task ProcessAsync(HttpRequest httpRequest, Activity activity, BotCallbackHandler callback, CancellationToken cancellationToken);
    }
}