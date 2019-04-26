using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockBotFrameworkHttpAdapter : IBotFrameworkHttpAdapter
    {
        public Task ProcessAsync(HttpRequest httpRequest, HttpResponse httpResponse, IBot bot, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}