using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockBot : IBot
    {
        public Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}