using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Auth;

namespace Microsoft.Bot.Builder.Skills.Tests.Mocks
{
    public class DummyMicrosoftAppCredentialsEx : MicrosoftAppCredentialsEx
    {
        public DummyMicrosoftAppCredentialsEx(string appId, string password, string scope)
            : base(appId, password, scope)
        {
        }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}