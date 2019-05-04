using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class MockServiceClientCredentials : IServiceClientCredentials
    {
		public Task<string> GetTokenAsync(bool forceRefresh = false)
		{
			return Task.FromResult(string.Empty);
		}

		public Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}